export function registerJwt(fastify) {
  fastify.register(
    import("fastify-jwt").then((m) => m.default),
    {
      secret: process.env.JWT_SECRET, // HS256 demo
      sign: { issuer: process.env.SERVICE_NAME, expiresIn: "2h" },
    }
  );
  fastify.decorate("auth", async (req, reply) => {
    try {
      await req.jwtVerify();
    } catch (e) {
      return reply.code(401).send({ message: "Unauthorized" });
    }
  });
}
