// RetailInsights on Azure Container Apps.
//   az deployment group create -g <rg> -f infra/main.bicep -p apiImage=... webImage=... postgresAdminPassword=...
targetScope = 'resourceGroup'

@description('Prefix of every resource name.')
@minLength(3)
@maxLength(16)
param prefix string = 'retailinsights'

param location string = resourceGroup().location

@description('API image, e.g. ghcr.io/<owner>/retailinsights-api:<sha>.')
param apiImage string

@description('Dashboard image, e.g. ghcr.io/<owner>/retailinsights-web:<sha>.')
param webImage string

@description('Registry of the images. Credentials are only needed for private packages.')
param registryServer string = 'ghcr.io'
param registryUsername string = ''
@secure()
param registryPassword string = ''

param postgresAdminLogin string = 'retailadmin'
@secure()
param postgresAdminPassword string

@description('Generate a demo sales history on first start.')
param seedDemoData bool = true

var suffix = uniqueString(resourceGroup().id)
var hasRegistryCredentials = !empty(registryPassword)
var registrySecrets = hasRegistryCredentials ? [
  {
    name: 'registry-password'
    value: registryPassword
  }
] : []
var registries = hasRegistryCredentials ? [
  {
    server: registryServer
    username: registryUsername
    passwordSecretRef: 'registry-password'
  }
] : []

resource logs 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${prefix}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${prefix}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: '${prefix}-pg-${suffix}'
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

resource postgresDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgres
  name: 'retailinsights'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Lets Azure services (the Container Apps environment) reach the server.
resource postgresFirewall 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: postgres
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

var postgresConnection = 'Host=${postgres.properties.fullyQualifiedDomainName};Database=${postgresDatabase.name};Username=${postgresAdminLogin};Password=${postgresAdminPassword};SSL Mode=Require'

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${prefix}-api'
  location: location
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      secrets: concat([
        {
          name: 'postgres-connection'
          value: postgresConnection
        }
      ], registrySecrets)
      registries: registries
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ConnectionStrings__RetailInsights'
              secretRef: 'postgres-connection'
            }
            {
              // Redis runs as a sidecar of the API (same replica, localhost).
              name: 'ConnectionStrings__Redis'
              value: 'localhost:6379,abortConnect=false'
            }
            {
              name: 'Seed__Enabled'
              value: string(seedDemoData)
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: 'https://${prefix}-web.${environment.properties.defaultDomain}'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health/live'
                port: 8080
              }
              periodSeconds: 15
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 10
            }
          ]
        }
        {
          name: 'redis'
          image: 'docker.io/library/redis:7-alpine'
          args: [
            '--save'
            ''
            '--appendonly'
            'no'
            '--maxmemory'
            '200mb'
            '--maxmemory-policy'
            'allkeys-lru'
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

resource web 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${prefix}-web'
  location: location
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
      }
      secrets: registrySecrets
      registries: registries
    }
    template: {
      containers: [
        {
          name: 'web'
          image: webImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'API_UPSTREAM'
              value: 'https://${api.properties.configuration.ingress.fqdn}'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 2
        rules: [
          {
            name: 'http'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

output apiUrl string = 'https://${api.properties.configuration.ingress.fqdn}'
output webUrl string = 'https://${web.properties.configuration.ingress.fqdn}'
