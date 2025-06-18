FROM fluent/fluentd:v1.16-debian-1

# Switch to root to install plugins
USER root

# Install the Elasticsearch plugin (record_transformer is built-in)
RUN fluent-gem install fluent-plugin-elasticsearch \
    && fluent-gem cleanup

# Switch back to fluentd user
USER fluentd