# Bob's Used Books Classic

## Overview
Bob's Used Books Classic is a backport of the [Bob's Used Books Sample Application](https://github.com/aws-samples/bobs-used-bookstore-sample). It started life as an ASP.NET MVC application targeting .NET Framework 4.8 and has since been migrated to ASP.NET Core on **.NET 10**.

The solution is composed of five projects:
- `Bookstore.Web` — ASP.NET Core MVC web application (`net10.0`)
- `Bookstore.Data` — EF Core 10 data access + file/image services (`net10.0`)
- `Bookstore.Domain` — business logic and DTOs (`net10.0`)
- `Bookstore.Common` — shared constants (`net10.0`)
- `Bookstore.Cdk` — AWS CDK infrastructure (`net10.0`)

## Prerequisites

### For local development
- The [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/) (to run SQL Server locally, and optionally to build/run the web container)
- A modern IDE, for example [Visual Studio Code](https://code.visualstudio.com/), [Visual Studio 2022](https://visualstudio.microsoft.com/vs/), or [JetBrains Rider](https://www.jetbrains.com/rider/)

The project works on Windows, macOS, and Linux — IIS and Windows containers are no longer required.

### For deployment to AWS
- An AWS IAM user or role with permission to deploy the CDK stacks
- The [AWS Cloud Development Kit (CDK)](https://docs.aws.amazon.com/cdk/v2/guide/getting_started.html)
- [Bootstrap](https://docs.aws.amazon.com/cdk/v2/guide/bootstrapping.html) your AWS environment by executing `cdk bootstrap`
- Docker (any host OS) — the ECS task now runs on **Linux/ARM64** Fargate, so Docker Desktop no longer needs to be switched to Windows containers

## Getting started (local)

1. Clone the repository and open the solution in your preferred IDE.
2. Start a local SQL Server:

   ```bash
   docker run --rm --name bobs-bookstore-mssql \
       -e ACCEPT_EULA=Y \
       -e MSSQL_SA_PASSWORD=BobsBookstore1! \
       -e MSSQL_PID=Developer \
       -p 1433:1433 \
       mcr.microsoft.com/mssql/server:2022-latest
   ```

3. Restore the `dotnet-ef` tool and apply the initial migration (creates the `BookStoreClassic` database and seeds reference data):

   ```bash
   dotnet tool restore
   dotnet ef database update \
       --project app/Bookstore.Data \
       --startup-project app/Bookstore.Web
   ```

4. Run the web application:

   ```bash
   dotnet run --project app/Bookstore.Web
   ```

5. Browse to `http://localhost:5080/` (or whichever URL the launch profile picks).

### Configuration

Runtime behaviour is controlled through `appsettings.json` in `app/Bookstore.Web/`. The top-level service settings select between a local, offline implementation and the AWS implementation of each integration:

```json
{
  "Services/Authentication": "local",
  "Services/Database": "local",
  "Services/FileService": "local",
  "Services/ImageValidationService": "local",
  "Services/LoggingService": "local"
}
```

Change a value from `"local"` to `"aws"` to switch that integration over to the AWS implementation. With the exception of the database service, you can mix and match freely from a local development environment. `appsettings.Development.json` overrides the defaults when `ASPNETCORE_ENVIRONMENT=Development` and provides a connection string that targets the Docker SQL Server above.

> \* The RDS for SQL Server database is deployed to a private subnet and is not reachable from outside the application's VPC.

## Amazon Cognito first run

Use the following credentials the first time you authenticate with the AWS authentication implementation:

* Username: **Admin**
* Password: **P@ssword1**

## Building and running the container

The repository ships a single cross-platform `Dockerfile` at the root. To build and run the image locally:

```bash
docker build -t bobs-bookstore:latest .

# Run the container and point it at the local SQL Server from step 2 above
docker run --rm -p 8080:8080 \
    -e ASPNETCORE_ENVIRONMENT=Development \
    -e ConnectionStrings__BookstoreDatabaseConnection="Server=host.docker.internal,1433;Database=BookStoreClassic;User Id=sa;Password=BobsBookstore1!;TrustServerCertificate=True;MultipleActiveResultSets=true;" \
    bobs-bookstore:latest
```

Browse to `http://localhost:8080/` to hit the containerised application.

## Deployment

The `BobsUsedBooksClassicECS` CDK stack containerises the application and deploys it to Amazon ECS on Fargate (Linux/ARM64). To deploy:

```bash
cdk deploy BobsUsedBooksClassicECS
```

When deployed to ECS, the task definition is wired to the AWS implementations of every integration except Amazon Cognito. The Cognito Hosted UI requires HTTPS redirects (except for `http://localhost`) and the ECS service is fronted by an HTTP ALB, so the container falls back to the local authentication service.

### Post-deployment checks

After a deployment, verify the following against the running stack:

- SSM Parameter Store values are loaded at startup (database connection string, Cognito metadata)
- `S3FileService` accepts uploads from the admin inventory screens
- `RekognitionImageValidationService` rejects non-compliant images
- The `aws` authentication path redirects to the Cognito Hosted UI over HTTPS
- Application logs appear in CloudWatch Logs via `AWS.Logger.NLog`

## Cleaning up

When you're done with the sample, delete the stacks to avoid ongoing charges:

* Via the console — delete all **BobsUsedBooksClassic** stacks from CloudFormation.
* Via the CLI — from the solution folder run `cdk destroy BobsUsedBooksClassic*`.
