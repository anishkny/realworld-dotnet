#!/usr/bin/env bash
set -euxo pipefail

PORT=5000
TIMEOUT=30000   # in milliseconds

npm run build
npm run stop > /dev/null 2>&1 || true
npm start &
npx wait-port http://localhost:${PORT} --output dots --timeout=${TIMEOUT}
npm test
npm run stop
npm run coverage-report
npm run check-coverage
