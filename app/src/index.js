const http = require('http');
const { add } = require('./math');

const durationBuckets = [0.05, 0.1, 0.2, 0.5, 1, 2];
const requestCounts = new Map();
const requestDurations = new Map();

function observe(labels, durationSeconds) {
  const key = JSON.stringify(labels);
  requestCounts.set(key, (requestCounts.get(key) || 0) + 1);

  const stats = requestDurations.get(key) || {
    sum: 0,
    count: 0,
    buckets: durationBuckets.map(() => 0)
  };

  stats.sum += durationSeconds;
  stats.count += 1;

  durationBuckets.forEach((bucket, index) => {
    if (durationSeconds <= bucket) {
      stats.buckets[index] += 1;
    }
  });

  requestDurations.set(key, stats);
}

function renderMetrics() {
  const lines = [
    '# HELP http_requests_total Total number of HTTP requests.',
    '# TYPE http_requests_total counter'
  ];

  for (const [key, count] of requestCounts.entries()) {
    const labels = JSON.parse(key);
    const labelString = Object.entries(labels)
      .map(([name, value]) => `${name}="${value}"`)
      .join(',');
    lines.push(`http_requests_total{${labelString}} ${count}`);
  }

  lines.push('# HELP http_request_duration_seconds HTTP request duration in seconds.');
  lines.push('# TYPE http_request_duration_seconds histogram');

  for (const [key, stats] of requestDurations.entries()) {
    const labels = JSON.parse(key);
    const baseLabels = Object.entries(labels)
      .map(([name, value]) => `${name}="${value}"`)
      .join(',');

    durationBuckets.forEach((bucket, index) => {
      lines.push(
        `http_request_duration_seconds_bucket{${baseLabels},le="${bucket}"} ${stats.buckets[index]}`
      );
    });

    lines.push(
      `http_request_duration_seconds_bucket{${baseLabels},le="+Inf"} ${stats.count}`
    );
    lines.push(`http_request_duration_seconds_sum{${baseLabels}} ${stats.sum.toFixed(6)}`);
    lines.push(`http_request_duration_seconds_count{${baseLabels}} ${stats.count}`);
  }

  return `${lines.join('\n')}\n`;
}

const server = http.createServer((req, res) => {
  const startedAt = process.hrtime.bigint();
  const path = new URL(req.url || '/', 'http://localhost').pathname;
  let statusCode = 200;

  try {
    if (path === '/metrics') {
      res.writeHead(200, {
        'Content-Type': 'text/plain; version=0.0.4; charset=utf-8'
      });
      res.end(renderMetrics());
      return;
    }

    if (path === '/health') {
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ status: 'healthy' }));
      return;
    }

    if (path === '/') {
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ status: 'ok', result: add(2, 3) }));
      return;
    }

    statusCode = 404;
    res.writeHead(statusCode, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ error: 'Not found' }));
  } finally {
    const durationSeconds = Number(process.hrtime.bigint() - startedAt) / 1_000_000_000;
    observe(
      {
        method: req.method || 'GET',
        path,
        status: String(statusCode)
      },
      durationSeconds
    );
  }
});

const PORT = process.env.PORT || 3000;
server.listen(PORT, () => console.log(`Server running on port ${PORT}`));