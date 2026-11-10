# Ads For All - Boards

A single-page bulletin board: a **React** SPA on top of a **.NET 10 REST API** that keeps the ads in a **local JSON file**.

- Browse every ad, newest first. **Search by title**, and filter by **category**, **price from–to** and **distance** (within 1–100 km of a place you pin on the map, or of your location).
- **Create, edit and delete** ads: the board is open to all, so anyone can change or remove any ad.
- **Bonus:** every ad has a map location, set by address search, clicking the map or dragging the pin. The board shows every ad on a map; search an address or click the map to pin your place, and the distance filter shows the ads near it.

![screen](docs/screen.jpg)

## Assumptions

**Every user can add, edit and delete any ad.** There are no accounts, so the server doesn't check who created an ad. In real life, users would need to register and sign in, and only the user who created an ad could edit or delete it. Other roles could be added too, such as an admin who can remove any ad. This can be discussed.

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

## Keeping the JSON file consistent

The API is assumed to run as **one server**. It keeps the ads in memory and is the only process that reads or writes `ads.json`. Writes (create / edit / delete) go one at a time through a `SemaphoreSlim`. Each one saves the file through a temp file and a rename, so a crash never leaves half a file.

If more than one server is needed, the servers would also need a lock between them, and a way to notice each other's writes. This can be discussed.

## Configuration (`appsettings.json`)

| Key | Default | |
|---|---|---|
| `Storage:FolderPath` | `data` | The folder that holds `ads.json` |
| `Storage:SeedFile` | `SeedData/ads.json` | Copied in when the folder is empty |
| `Storage:LockTimeoutSeconds` | `15` | How long a write waits for the lock before 503 |
| `Storage:MaxPins` | `2000` | Most pins per map request |
| `Cors:Origins` | `http://localhost:5273` | Only needed when the client is hosted on another origin |
