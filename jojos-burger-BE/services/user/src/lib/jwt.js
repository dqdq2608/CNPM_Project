import fastifyJwt from '@fastify/jwt';

export function registerJwt(fastify) {
  fastify.register(fastifyJwt, {
    secret: process.env.JWT_SECRET,
    sign: { issuer: process.env.SERVICE_NAME, expiresIn: '2h' }
  });

  fastify.decorate('auth', async (req, reply) => {
    try { await req.jwtVerify(); }
    catch { return reply.code(401).send({ message: 'Unauthorized' }); }
  });
}
