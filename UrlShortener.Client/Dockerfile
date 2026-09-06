# STAGE 1: Build React SPA
FROM node:22-alpine AS build
WORKDIR /app

COPY UrlShortener.Client/package*.json ./
RUN npm ci

COPY UrlShortener.Client/ ./
RUN npm run build

# STAGE 2: Serve via Nginx
# nginx:alpine — стандартный официальный суперлегкий образ для отдачи SPA и Reverse Proxy
FROM nginx:1.27-alpine AS final

COPY --from=build /app/dist /usr/share/nginx/html
COPY UrlShortener.Client/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]