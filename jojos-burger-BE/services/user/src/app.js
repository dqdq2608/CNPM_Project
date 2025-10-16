import Fastify from "fastify";
import fastifySensible from "fastify-sensible";
import rateLimit from "@fastify/rate-limit";

import { registerJwt } from "./lib/jwt.js";
import { registerMetrics } from "./lib/metrics.js";
import health from "./health.js";
import authRoutes from "./routes/auth.routes.js";

export function buildApp() {
  const app = Fastify({ logger: true });

  app.register(fastifySensible);
  app.register(rateLimit, { max: 200, timeWindow: "1 minute" });

  registerJwt(app);
  registerMetrics(app);

  app.register(health);
  app.register(authRoutes, { prefix: "/" });

  return app;
}
