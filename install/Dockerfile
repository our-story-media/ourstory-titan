FROM node:8-stretch
ARG TARGETPLATFORM
RUN echo "BUILDING FOR $TARGETPLATFORM"
RUN if [ "$TARGETPLATFORM" = "linux/arm/v7" ] ; then wget https://andyfelong.com/downloads/mongodb_stretch_3_0_14_core.zip && \
        unzip mongodb_stretch_3_0_14_core.zip && \
        adduser --ingroup nogroup --shell /etc/false --disabled-password --gecos "" \
        --no-create-home mongodb && \
        cd core && \
        chown root:root mongo* && \
        chmod 755 mongo* && \
        strip mongo* && \
        cp -p mongo* /usr/bin && \
        cd .. && \
        rm -R core && \
        rm mongodb_stretch_3_0_14_core.zip ; \
    fi


RUN PACKAGES="" && \ 
    if [ ! "$TARGETPLATFORM" = "linux/arm/v7" ] ; then PACKAGES="mongodb" ; fi && \
    apt-get update -q && \
    apt-get install -q -o Dpkg::Options::="--force-confdef" -o Dpkg::Options::="--force-confold" -y nginx redis-server beanstalkd libav-tools melt $PACKAGES

RUN git clone --depth=1 https://github.com/our-story-media/ourstory-server && git clone --depth=1 https://github.com/our-story-media/ourstory-worker

WORKDIR /ourstory-server

RUN npm install grunt-cli && \
    npm install --silent && \
    npm run buildgrunt && \
    mkdir -p ./upload/ && \
	rm -R ./assets/music/ && \
	rm ./Gruntfile.js && \
	rm -R ./tasks && \
    npm prune --production && \
	npm uninstall grunt-cli && \
	npm cache clean --force

WORKDIR /ourstory-worker
RUN npm i --silent && \
    mkdir ~/.fonts && cp -r ./fonts/* ~/.fonts && chmod -R 644 ~/.fonts && fc-cache && \
    mkdir -p /usr/src && \
    ln -s /ourstory-server /usr/src/app && \
    mkdir /templates && \
    mkdir -p /etc/nginx/vhost.d && \
    rm /etc/nginx/sites-enabled/default && \
    ln -s /ourstory-server/docker/bootlegger.conf /etc/nginx/vhost.d/default && \
    ln -sb /ourstory-server/fallback/index.html /usr/share/nginx/html/index.html && \
    ln -s /ourstory-server/docker/server.local.conf /etc/nginx/conf.d/default.conf && \
    mkdir -p /data/db && \
    mkdir -p /redis

VOLUME /data/db

VOLUME /redis

COPY "entrypoint.sh" .

RUN chmod +x entrypoint.sh

CMD [ "./entrypoint.sh" ]