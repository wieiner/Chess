# TLS Certificate Notes

P4B does not issue certificates or manage DNS.

Use a deployment-local certificate manager such as certbot or a platform-managed certificate system. The repository contains only placeholder snippets. Never commit:

- `.key` files;
- private cert bundles;
- real domain certificate paths containing private material;
- deployment secrets.
