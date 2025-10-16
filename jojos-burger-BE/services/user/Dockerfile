FROM node:20-slim
WORKDIR /app

# Cài deps (cả dev) để có 'prisma' cho bước generate
COPY package*.json ./
RUN npm ci

# Copy Prisma schema và generate client
COPY prisma ./prisma
RUN npx prisma generate

# Copy code
COPY src ./src

ENV NODE_ENV=production
EXPOSE 3001

# Loại bỏ dev deps trước khi chạy (giảm kích thước image)
RUN npm prune --omit=dev

CMD ["node", "src/server.js"]
