FROM fluentd:v1.16-1

USER root

RUN apk add --update --virtual .build-deps \
    sudo build-base ruby-dev \
    && gem install fluent-plugin-elasticsearch \
    && gem install fluent-plugin-multi-format-parser \
    && gem install fluent-plugin-prometheus \
    && gem install fluent-plugin-grafana-loki \
    && gem install fluent-plugin-record-modifier \
    && gem install fluent-plugin-rewrite-tag-filter \
    && gem cleanup \
    && apk del .build-deps

COPY conf/fluent.conf /fluentd/etc/

USER fluent