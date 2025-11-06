#!/usr/bin/env bash
set -euxo pipefail

PORT=5000
DELAY=5000        # in milliseconds
INTERVAL=1000     # in milliseconds
TIMEOUT=30000   # in milliseconds

npm run build
npm run stop > /dev/null 2>&1 || true
npm start &
npx wait-on --verbose --delay $DELAY --interval $INTERVAL --timeout $TIMEOUT http://localhost:$PORT/
npm test
npm run stop
npm run coverage-report
npm run check-coverage
