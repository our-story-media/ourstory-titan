FROM node:12-stretch

WORKDIR /build

RUN npm config set unsafe-perm true && npm install -g pkg

COPY . .

RUN pkg indaba-supervisor.js --targets armv7