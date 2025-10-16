import "dotenv/config";
import { buildApp } from "./app.js";

const port = process.env.PORT || 3001;
const app = buildApp();

app.listen({ port, host: "0.0.0.0" })
  .then(() => app.log.info(`🚀 ${process.env.SERVICE_NAME} up on :${port}`))
  .catch(err => { app.log.error(err); process.exit(1); });
