#!/usr/bin/env bash

# Stop execution on first error encountered
set -o errexit -o pipefail -o noclobber

# Parse command line arguments
for ARG in "$@"; do
  case $ARG in
    -h=*|--host=*)
      HOST="${ARG#*=}"
      shift
      ;;
    -*|--*)
      echo "Unknown argument '$ARG'" >&2
      exit 1
      ;;
    *)
      ;;
  esac
done

# Validate command line arguments and set defaults
if [ -z $HOST ]; then
  echo "No host provided. Please provide a host to run health checks against. Sample usage '$0 -h=example.com'" >&2
  exit 1
fi

# Global scope variables
RESPONSE_BODY="{}" # The last response body received
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'

# Function to send curl request, validate response status, and parse and return response body
send_and_validate() {
    local verb=$1
    local url_path=$2
    local body=$3

    local response=$(curl -s -w "%{http_code}" \
                -H "X-Forwarded-For: 127.0.0.1" \
                -H 'Content-Type: application/json' \
                --retry 5 \
                --retry-connrefused \
                --location \
                -X "$verb" "http://$HOST$url_path" \
                -d "$body")

    local http_code=${response: -3}
    RESPONSE_BODY=${response:: -3}

    if ! [[ "$http_code" =~ ^2 ]]; then
        echo -e " ... ${RED}FAILED"
        echo -e "Got response '$http_code: $RESPONSE_BODY'"

        exit 1
    else
        echo -e " ... ${GREEN}OK"
    fi
}



# Start test execution
echo -ne "${YELLOW}Test #1 --- Health check"
send_and_validate "GET" "/api/live"


echo -ne "${YELLOW}Test #2 --- Get upcoming concerts"
send_and_validate "GET" "/api/concerts/?take=10"
CONCERT_ID=$(echo $RESPONSE_BODY | jq -r ".items[0].id")


echo -ne "${YELLOW}Test #3 --- Get concert by ID"
send_and_validate "GET" "/api/concerts/$CONCERT_ID"


echo -ne "${YELLOW}Test #4 --- Create new user"
CREATE_USER_REQUEST='{
    "email": "healthcheck@example.com",
    "phone": "0123456789",
    "displayName": "Health Check"
}'
send_and_validate "POST" "/api/users" "$CREATE_USER_REQUEST"
USER_ID=$(echo $RESPONSE_BODY | jq -r ".id")


echo -ne "${YELLOW}Test #5 --- Get user by ID"
send_and_validate "GET" "/api/users/$USER_ID"


echo -ne "${YELLOW}Test #6 --- Put item in cart"
PUT_ITEM_REQUEST="{
    \"concertId\": \"$CONCERT_ID\",
    \"quantity\": 3
}"
send_and_validate "PUT" "/api/users/$USER_ID/carts" "$PUT_ITEM_REQUEST"


echo -ne "${YELLOW}Test #7 --- Create order (checkout cart)"
CREATE_ORDER_REQUEST='{
    "cardholder": "Example User",
    "cardNumber": "378282246310005",
    "securityCode": "123",
    "expirationMonthYear": "1029"
}'
send_and_validate "POST" "/api/users/$USER_ID/orders" "$CREATE_ORDER_REQUEST"


echo -ne "${YELLOW}Test #8 --- Get orders"
send_and_validate "GET" "/api/users/$USER_ID/orders/?take=3&skip=0"


echo -e "${GREEN}All tests executed successfully!"
