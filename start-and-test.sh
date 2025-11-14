#!/usr/bin/env bash
set -euxo pipefail

PORT=3000
TIMEOUT=30000   # in milliseconds

npm run build
npm run stop > /dev/null 2>&1 || true
npm run start:only &
npx wait-port http://localhost:${PORT}/api --output dots --timeout=${TIMEOUT}
npm run test:only
npm run stop
npm run coverage-report
npm run check-coverage
