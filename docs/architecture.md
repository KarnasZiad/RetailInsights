# Architecture de RetailInsights

## 1. Contexte

```mermaid
flowchart LR
    caisse[Caisses / e-commerce] -- POST /api/sales --> api
    manager[Directeur réseau,<br/>responsable magasin] --> web
    web[Tableau de bord React<br/>nginx] -- /api --> api[RetailInsights API<br/>.NET 8]
    api --> pg[(PostgreSQL<br/>tickets, lignes)]
    api <--> redis[(Redis<br/>KPI calculés)]
```

## 2. Découpage en couches

```mermaid
flowchart TB
    subgraph Api["RetailInsights.Api"]
        C[Controllers<br/>Swagger / XML docs]
        EH[ApiExceptionHandler<br/>ProblemDetails]
    end
    subgraph Core["RetailInsights.Core (aucune dépendance)"]
        KS[KpiService<br/>orchestration + cache-aside]
        KC[KpiCalculator<br/>règles pures]
        DR[DateRange, SalesTotals]
        ING[SalesIngestionService]
        P1[[ISalesAggregates]]
        P2[[IKpiCache]]
        P3[[ISalesWriter]]
    end
    subgraph Infra["RetailInsights.Infrastructure"]
        AG[SalesAggregates<br/>EF Core GROUP BY]
        CA[DistributedKpiCache<br/>Redis, clés versionnées]
        SW[SalesWriter]
        GEN[DemoDataGenerator]
    end
    C --> KS & ING
    KS --> KC & P1 & P2
    ING --> P3 & P2
    AG -.implémente.-> P1
    CA -.implémente.-> P2
    SW -.implémente.-> P3
```

**Répartition du calcul.** PostgreSQL fait ce qu'il fait le mieux : filtrer sur l'index
`(business_date, store_id)` et agréger (`SUM`, `COUNT`, `GROUP BY`). Aucune ligne de vente
n'est chargée en mémoire. Les ratios (panier moyen, indice de vente, prix moyen), les
évolutions, les parts, les classements et les regroupements par semaine ou par mois sont
calculés dans `KpiCalculator`, du code pur couvert par des tests unitaires.

**Totaux dénormalisés.** Chaque ticket stocke son montant et son nombre d'articles : les KPI
de synthèse, par magasin, par jour et par canal n'ont besoin d'aucune jointure. Seul le top
produits lit les lignes.

## 3. Cache Redis (cache-aside, clés versionnées)

```mermaid
sequenceDiagram
    participant D as Dashboard
    participant A as API (KpiService)
    participant R as Redis
    participant P as PostgreSQL

    D->>A: GET /api/kpis/summary?from&to&storeId
    A->>R: GET kpi:version
    A->>R: GET kpi:{version}:summary:{période}:{magasin}
    alt présent
        R-->>A: JSON
        A-->>D: 200, X-Cache: HIT
    else absent
        A->>P: 2 agrégations (période + période précédente)
        A->>A: KpiCalculator
        A->>R: SET ... EX 600
        A-->>D: 200, X-Cache: MISS
    end

    Note over A,R: POST /api/sales → SET kpi:version = nouvelle valeur<br/>toutes les anciennes clés deviennent inaccessibles<br/>et expirent seules (TTL), sans SCAN ni DEL
```

Choix :

- **Clé = type de KPI + période + magasin (+ granularité, classement)** : deux tableaux de bord sur la même vue partagent la même entrée.
- **Invalidation par version** plutôt que par suppression : une seule écriture, O(1), sans parcourir les clés ; les entrées périmées sont libérées par leur TTL et par la politique `allkeys-lru`.
- **TTL de 10 minutes** (configurable `KpiCache:TimeToLive`) : borne la fraîcheur même si une écriture contournait l'API.
- **Repli** : sans chaîne de connexion Redis, un cache mémoire est utilisé (développement local).

## 4. Pipeline CI/CD

```mermaid
flowchart LR
    push[push / PR] --> bt
    subgraph bt[build-test]
        b1[restore + build] --> b2[xUnit unitaires]
        b2 --> b3[xUnit intégration<br/>Testcontainers PG + Redis]
        b3 --> b4[couverture<br/>coverlet + ReportGenerator]
        b4 --> b5[SonarCloud<br/>qualité + couverture]
    end
    push --> web[web: npm ci + build]
    push --> infra[infra: az bicep build]
    bt & web & infra --> docker[images Docker<br/>api + web]
    docker -- main uniquement --> ghcr[(GHCR)]
    ghcr --> deploy[deploy<br/>OIDC → Azure<br/>az deployment group create]
    deploy --> smoke[smoke test<br/>/health/ready]
```

- Les pull requests exécutent tout sauf la publication des images et le déploiement.
- SonarCloud s'active dès que `SONAR_TOKEN` (secret) et `SONAR_ORGANIZATION` / `SONAR_PROJECT_KEY` (variables) sont définis.
- Le déploiement s'active dès que les variables Azure sont définies ; l'authentification se fait par **OpenID Connect** (fédération d'identité), aucun secret Azure n'est stocké dans GitHub.
- L'infrastructure est déclarative (Bicep) et redéployée à chaque livraison : le déploiement est idempotent et la nouvelle image crée une nouvelle révision Container Apps.

## 5. Déploiement Azure

```mermaid
flowchart TB
    user((Utilisateur)) -- HTTPS --> webapp
    subgraph rg[Resource group]
        subgraph env[Container Apps environment]
            webapp[retailinsights-web<br/>nginx + React<br/>0 à 2 réplicas]
            subgraph apiapp[retailinsights-api]
                apic[api .NET 8]
                redisc[redis sidecar]
            end
        end
        pg[(Azure Database for PostgreSQL<br/>Flexible Server B1ms)]
        la[Log Analytics]
    end
    webapp -- /api via HTTPS --> apic
    apic -- localhost:6379 --> redisc
    apic -- SSL --> pg
    env -. logs .-> la
    ghcr[(GHCR)] -. images .-> env
```

- **Redis en sidecar** de l'API : simple et sans coût supplémentaire pour une démo. Le cache est local à la réplica (l'API est fixée à une réplica). Pour monter en charge : Azure Managed Redis, en changeant uniquement `ConnectionStrings__Redis`.
- **Sondes** : liveness sur `/health/live`, readiness sur `/health/ready` (PostgreSQL + Redis).
- **Front** sans URL d'API figée dans le bundle : nginx proxifie `/api` vers l'URL fournie par `API_UPSTREAM`, la même image tourne en local et sur Azure.

## 6. Décisions

| Décision | Alternative écartée | Raison |
|---|---|---|
| Agréger en SQL, dériver en C# | Charger les tickets et calculer en mémoire | Volume : seules quelques lignes agrégées transitent |
| Totaux dénormalisés sur le ticket | Toujours joindre les lignes | Les KPI les plus consultés sans jointure |
| Cache-aside à clés versionnées | Suppression des clés par motif | Invalidation O(1), aucun `SCAN` sur Redis |
| Container Apps | AKS, App Service | Conteneurs sans gestion de cluster, scale-to-zero, révisions |
| OIDC pour Azure | Secret de service principal | Aucun secret longue durée dans GitHub |
