# FYP Backend Java - Shared Response-Agent Data Store

This replacement backend keeps the same Spring Boot project name and port as your current backend, but changes the architecture so the response-agent JSON is stored once as general `SecurityEvent` data and reused by all pages.

## Main change

The response agent should send data to:

```text
POST http://localhost:5000/api/events
```

The old endpoint still works too:

```text
POST http://localhost:5000/api/alerts
```

But it is now only a backward-compatible alias. It saves into the shared event store, not into an alerts-only list.

## API endpoints

```text
GET  /api/events              all received response-agent events
POST /api/events              receive one event, an array of events, or an object with items/events/alerts
DELETE /api/events            clear in-memory events

GET  /api/alerts              alert view filtered from shared events
POST /api/alerts              backward-compatible receive endpoint

GET  /api/dashboard           dashboard summary from shared events
GET  /api/reports             report summary from shared events
GET  /api/detection-activity  activity list from shared events
GET  /api/status              dynamic system status from shared events
```

## Important notes

- Data is stored in memory for now. Restarting the backend clears the events.
- The dashboard timeline uses the last 24 hours, which is better for your dashboard overview.
- Recent alert objects now include `processName` and `parentProcessName` if the response-agent JSON contains process fields.
- The backend accepts flexible JSON keys, such as `rule_name` or `ruleName`, `response_taken` or `responseTaken`, `process_name` or `processName`.

## Run

```bash
cd backend-java
mvn spring-boot:run
```
