FROM node:20 AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci || yarn --frozen-lockfile
COPY . .
RUN npm run build || yarn build

FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80