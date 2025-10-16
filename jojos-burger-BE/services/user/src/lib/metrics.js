import client from "prom-client";

export function registerMetrics(fastify) {
  const register = new client.Registry();
  client.collectDefaultMetrics({
    register,
    labels: { service: process.env.SERVICE_NAME },
  });

  // common HTTP duration histogram
  const httpDuration = new client.Histogram({
    name: "http_request_duration_seconds",
    help: "Request latency",
    labelNames: ["method", "route", "status_code", "service"],
    buckets: [0.05, 0.1, 0.2, 0.5, 1, 2, 5],
  });
  register.registerMetric(httpDuration);

  // business counters
  const userSignup = new client.Counter({
    name: "user_signup_total",
    help: "Total signups",
  });
  const userLogin = new client.Counter({
    name: "user_login_total",
    help: "Total logins",
  });
  register.registerMetric(userSignup);
  register.registerMetric(userLogin);

  // hook timing
  fastify.addHook("onResponse", async (req, reply) => {
    const route = req.routerPath || req.url;
    const seconds = (reply.getResponseTime?.() ?? 0) / 1000;
    httpDuration
      .labels({
        method: req.method,
        route,
        status_code: String(reply.statusCode),
        service: process.env.SERVICE_NAME,
      })
      .observe(seconds);
  });

  // expose /metrics
  fastify.get("/metrics", async (_req, reply) => {
    reply.header("Content-Type", register.contentType);
    return register.metrics();
  });

  // expose counters to controllers
  fastify.decorate("metrics", { userSignup, userLogin });
}
