# Generic Export Module

[![CI status](https://github.com/VirtoCommerce/vc-module-export/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-export/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-export&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-export) [![Reliability rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-export&metric=reliability_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-export) [![Security rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-export&metric=security_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-export) [![Sqale rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-export&metric=sqale_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-export)

## Overview

The Generic Export module provides a pluggable, provider-based export infrastructure for the Virto Commerce platform. It allows any module to register its domain entities as exportable types and then export them to different file formats (JSON, CSV) via a unified REST API. Export operations run as Hangfire background jobs with real-time progress reporting through push notifications, and the resulting files are persisted to the platform's blob storage for subsequent download.

## Key Features

* **Pluggable export type registry** — Other modules register their exportable entity types at startup; the export module discovers and exposes them through a single API.
* **Multiple export providers** — Ships with JSON (hierarchical) and CSV (tabular) providers out of the box; additional providers can be added via DI.
* **Background job execution** — Export tasks are queued as Hangfire background jobs, keeping API responses non-blocking and supporting cancellation.
* **Real-time progress notifications** — Push notifications report export progress (processed/total counts, errors, completion) to the calling client.
* **Property-level filtering** — Callers can select which properties to include in the export, and the module strips excluded properties before writing.
* **Tabular conversion** — Entities that implement `ITabularConvertible` can be flattened for CSV export while retaining their full hierarchy for JSON export.
* **Paged data retrieval** — A generic `ExportPagedDataSource` base class handles pagination, so consuming modules only need to implement the data-fetch logic.
* **Fluent type registration** — `ExportedTypeDefinitionBuilder` and its extension methods provide a fluent API for registering export types with metadata, data sources, and groups.
* **Configurable file naming** — Export file names are generated from a configurable template with timestamp formatting.
* **OR-based authorization** — The `AuthorizeAny` attribute allows endpoints to accept any one of several permissions (e.g., platform export OR module download).

## Configuration

### Application Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Export.FileNameTemplate` | ShortText | `export_{0:yyyyMMddHHmmss}` | Template for generated export file names. `{0}` is replaced with the UTC timestamp. |

### Permissions

| Permission | Description |
|------------|-------------|
| `export:access` | Grants access to list export types, preview data, start export tasks, and cancel running exports. |
| `export:download` | Grants access to download exported files (also accepted: `platform:export`). |

## Architecture

```
┌───────────────────────────────────────────────────────────┐
│                        Web Layer                          │
│  ExportController · ExportJob · Module                    │
│  (VirtoCommerce.ExportModule.Web)                         │
├──────────────┬────────────────────────┬───────────────────┤
│  CSV Provider│      Data Layer        │   JSON Provider   │
│  CsvExport-  │  DataExporter          │   JsonExport-     │
│  Provider    │  KnownExportTypes-     │   Provider        │
│  Metadata-   │    Service             │   ObjectDiscrim-  │
│  FilteredMap │  ExportProviderFactory │   inatorJson-     │
│              │  ExportPagedDataSource │   Converter       │
│  (CsvProvider│  ExportFileStorage     │                   │
│   project)   │  ExportedTypeDefini-   │  (JsonProvider    │
│              │    tionBuilder         │   project)        │
│              │  (VirtoCommerce.       │                   │
│              │   ExportModule.Data)   │                   │
├──────────────┴────────────────────────┴───────────────────┤
│                       Core Layer                          │
│  Domain Models · Service Interfaces · Settings            │
│  Permissions · ExportDataQuery · IExportProvider           │
│  (VirtoCommerce.ExportModule.Core)                        │
└───────────────────────────────────────────────────────────┘
```

> **Note:** This module has no database layer — it is a pure infrastructure/framework module. Data is sourced from other modules that register their paged data sources.

### Key Flow

1. During startup, consuming modules register their exportable types via `IKnownExportTypesRegistrar.RegisterType()`, supplying an `ExportedTypeDefinition` with metadata and a paged data source factory.
2. A client retrieves available export types by calling `GET /api/export/knowntypes`.
3. The client optionally previews data via `POST /api/export/data` with an `ExportDataRequest`.
4. The client starts an export by calling `POST /api/export/run` with the export type name, data query, and chosen provider name.
5. The controller creates an `ExportPushNotification` and enqueues a Hangfire background job (`ExportJob`).
6. `ExportJob` uses `IExportProviderFactory` to create the requested provider (JSON or CSV) and opens a write stream via `IExportFileStorage`.
7. `DataExporter` resolves the `ExportedTypeDefinition`, creates the `IPagedDataSource`, and iterates through pages of data.
8. For each record, properties are filtered to the requested subset, tabular conversion is applied if needed, and the record is written via the `IExportProvider`.
9. Progress callbacks update the push notification in real time (processed count, errors, description).
10. On completion, the download URL (`/api/export/download/{fileName}`) is set on the notification.
11. The client downloads the file via `GET /api/export/download/{fileName}`, which streams it from blob storage.

## Components

### Projects

| Project | Layer | Purpose |
|---------|-------|---------|
| VirtoCommerce.ExportModule.Core | Core | Domain models, service interfaces, permissions, settings, and constants |
| VirtoCommerce.ExportModule.Data | Data | Service implementations, paged data source base class, security utilities, and extension methods |
| VirtoCommerce.ExportModule.CsvProvider | Provider | CSV export provider using CsvHelper with metadata-driven column mapping |
| VirtoCommerce.ExportModule.JsonProvider | Provider | JSON export provider using Newtonsoft.Json with type discriminator support |
| VirtoCommerce.ExportModule.Web | Web | REST API controller, Hangfire background job, and module DI registration |
| VirtoCommerce.ExportModule.Tests | Tests | Unit tests |

### Key Services

| Service | Interface | Responsibility |
|---------|-----------|----------------|
| `DataExporter` | `IDataExporter` | Orchestrates the full export pipeline: resolves the data source, iterates pages, filters properties, and writes records via the provider |
| `KnownExportTypesService` | `IKnownExportTypesRegistrar`, `IKnownExportTypesResolver` | Thread-safe registry of exportable type definitions; allows registration and lookup by type name |
| `ExportProviderFactory` | `IExportProviderFactory` | Selects and instantiates the appropriate `IExportProvider` based on the provider name in the request |
| `ExportFileStorage` | `IExportFileStorage` | Generates export file names from the configured template and reads/writes files via the platform blob storage provider |
| `ExportPagedDataSource<TDataQuery, TSearchCriteria>` | `IPagedDataSource` | Abstract base class for paginated data retrieval; consuming modules subclass it to supply their own fetch logic |
| `ExportedTypeDefinitionBuilder` | — | Fluent builder for constructing `ExportedTypeDefinition` instances with metadata, data sources, and groups |
| `CsvExportProvider` | `IExportProvider` | Writes exportable entities as CSV rows using CsvHelper; requires tabular (flat) data |
| `JsonExportProvider` | `IExportProvider` | Writes exportable entities as a JSON array with type discriminators; supports hierarchical data |
| `ExportJob` | — | Hangfire background job that wires together the provider, file storage, and data exporter with progress reporting |

### REST API

Base route: `api/export`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/export/knowntypes` | Returns all registered exportable type definitions |
| GET | `/api/export/providers` | Returns all available export providers |
| POST | `/api/export/data` | Fetches a page of exportable entities for preview based on the data request |
| POST | `/api/export/run` | Starts an export background job and returns a push notification with the job ID |
| POST | `/api/export/task/cancel` | Cancels a running export job by its Hangfire job ID |
| GET | `/api/export/download/{fileName}` | Downloads a previously exported file by file name |

## Documentation

* [Generic Export module user documentation](https://docs.virtocommerce.org/platform/user-guide/generic-export/overview/)
* [REST API Reference](https://virtostart-demo-admin.govirto.com/docs/index.html?urls.primaryName=VirtoCommerce.Export)
* [View on GitHub](https://github.com/VirtoCommerce/vc-module-export)

## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download Latest Release](https://github.com/VirtoCommerce/vc-module-export/releases/latest)

## License

Copyright (c) Virto Solutions LTD. All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at <https://virtocommerce.com/opensourcelicense>.

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
