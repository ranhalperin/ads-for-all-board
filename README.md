# Ads For All - Boards

A single-page bulletin board: a **React** SPA on top of a **.NET 10 REST API** that keeps the ads in a **local JSON file**.

- Browse every ad, newest first. **Search by title**, and filter by **category**, **price from–to** and **distance** (within 1–100 km of a place you pin on the map, or of your location).
- **Create, edit and delete** ads: the board is open to all, so anyone can change or remove any ad.
- **Bonus:** every ad has a map location, set by address search, clicking the map or dragging the pin. The board shows every ad on a map; search an address or click the map to pin your place, and the distance filter shows the ads near it.

![screen](docs/screen.jpg)

## Run it

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Node.js 20+](https://nodejs.org).

```bash
# 1. API: http://localhost:5280 (Swagger at /swagger)
cd server/BulletinBoard.Api
dotnet run
```

```bash
# 2. Client: http://localhost:5273 (proxies /api to the API)
cd client
npm install
npm run dev
```

The first run creates `server/BulletinBoard.Api/data/` from `SeedData/ads.json` (12 sample ads around Israel).

### Tests

```bash
dotnet test                       # API: 30 tests (filters, file store, locking, HTTP)
cd client && npm test             # client: query building, validation, address labels
cd client && npm run typecheck
```

### Load 10,000 ads

```bash
dotnet run --project tools/BulletinBoard.DataGen -c Release -- 10000 server/BulletinBoard.Api/data
```

Stop the API first: it reads `ads.json` once, when it starts.

## Keeping the JSON file consistent

The API is assumed to run as **one server**. It keeps the ads in memory and is the only process that reads or writes `ads.json`. Writes (create / edit / delete) go one at a time through a `SemaphoreSlim`. Each one saves the file through a temp file and a rename, so a crash never leaves half a file.

If more than one server is needed, the servers would also need a lock between them, and a way to notice each other's writes. This can be discussed.

## Size limits

Sized for **up to about 100,000 ads**. There is no hard limit: past that, every write and every reload just gets slower (see below).

Measured on a laptop with 100,000 generated ads (47.8 MB file):

| Operation | Time |
|---|---|
| Load at the first request (whole file) | ~0.8–1.2 s, once |
| Search / filters / distance (one page) | 20–100 ms |
| Map pins for an area | ~35 ms |
| Create / edit / delete (rewrites the whole file under the lock) | ~0.5 s |
| 5 creates at once | queued: 0.4 s → 2.0 s, all saved |
| API process memory | ~190 MB |

The limit comes from the single file: every write rewrites all of it. Beyond ~100K:

- **Must stay JSON:** split into shards (for example by map area: `32.0_34.7.jsonl`), each written on its own, and keep only a slim index in memory (id, title, category, price, lat/lng, contact name, date). That's about 150–200 bytes per ad, roughly 200 MB for 1M ads, and full ads are read only for the page being shown.
- **Free choice:** a database (PostgreSQL + PostGIS, MongoDB with a geo index, DynamoDB). Only a new `IAdRepository` is needed: it turns the same `AdSearch` into a database query (`WHERE`, `ORDER BY created DESC LIMIT`, a geo index). The controller, service and client stay the same.

## Configuration (`appsettings.json`)

| Key | Default | |
|---|---|---|
| `Storage:FolderPath` | `data` | The folder that holds `ads.json` |
| `Storage:SeedFile` | `SeedData/ads.json` | Copied in when the folder is empty |
| `Storage:LockTimeoutSeconds` | `15` | How long a write waits for the lock before 503 |
| `Storage:MaxPins` | `2000` | Most pins per map request |
| `Cors:Origins` | `http://localhost:5273` | Only needed when the client is hosted on another origin |
