# hhh

A small basic dotnet h3 server, built with ioxide, an early experimental io_uring runtime for dotnet plus the rocksolid ngtcp2 and nghttp3 libraries.

You won't find much use to it, browsers don't support h3 directly so this server won't work.

Use curl3 probably with -k to test it out.

See the samples, have fun!

Tip: This server is a shared nothing no work stealing model so don't use any dotnet async APIs or anything that schedules to threadpool, instead use ioxide modules that ride the io_uring worker and never hop out its thread.

This project is a very high performance webserver example, not tailored for prod.


## Numbers

The sample against nginx, both serving the same 14-byte body over HTTP/3. One reactor against one
nginx worker, loopback, `h2load-h3 -D 8 -c 16 -m 16 -t 1`, three runs each:

| | req/s | failed |
|---|---:|---:|
| hhh | 363,020 / 368,210 / 360,994 | 0 |
| nginx 1.31.3 | 241,696 / 230,632 / 236,482 | 0 |

nginx sends more than twice the headers - 185 bytes across 7 (`server`, `date`, `last-modified`,
`etag`, `accept-ranges`) against hhh's 76 across 3.

nginx needs `keepalive_requests` raised. At its default of 1000 it recycles connections mid-run
and the benchmark measures handshakes.