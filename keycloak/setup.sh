#!/bin/sh
# ============================================================
# Keycloak Setup Script
# Runs automatically after keycloak container is healthy
# ============================================================
set -e

KEYCLOAK_URL="http://keycloak:8085"      # ← use service name, not localhost
KEYCLOAK_URL_HEALTH="http://keycloak:9000"      # ← url for health

ADMIN_USER="${KC_BOOTSTRAP_ADMIN_USERNAME:-admin}"
ADMIN_PASS="${KC_BOOTSTRAP_ADMIN_PASSWORD:-Mauri@22}"
REALM="orderpay"

echo ""
echo "════════════════════════════════════════"
echo "   🔐 OrderPay — Keycloak Setup"
echo "════════════════════════════════════════"
echo ""

# ---- Step 1: Wait for Keycloak ----
echo "⏳  Waiting for Keycloak..."
MAX_RETRIES=30
RETRY=0
until curl -sf \
    --connect-timeout 10 \
    --max-time 30 \
    "${KEYCLOAK_URL_HEALTH}/health/ready" > /dev/null 2>&1; do
    RETRY=$((RETRY + 1))
    if [ "${RETRY}" -ge "${MAX_RETRIES}" ]; then
        echo "❌  Keycloak not ready after ${MAX_RETRIES} attempts."
        exit 1
    fi
    echo "    attempt ${RETRY}/${MAX_RETRIES} — retrying in 3s..."
    sleep 3
done
echo "✅  Keycloak is ready."
echo ""

# ---- Step 2: Get admin token ----
echo "🔑  Getting admin token..."
TOKEN_RESPONSE=$(curl -sf \
    --connect-timeout 10 \
    --max-time 30 \
    -d "client_id=admin-cli" \
    -d "username=${ADMIN_USER}" \
    -d "password=${ADMIN_PASS}" \
    -d "grant_type=password" \
    "${KEYCLOAK_URL}/realms/master/protocol/openid-connect/token")

ACCESS_TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token')

if [ -z "$ACCESS_TOKEN" ]; then
    echo "❌  Failed to get admin token."
    exit 1
fi
echo "✅  Admin token obtained."
echo ""

AUTH="Authorization: Bearer ${ACCESS_TOKEN}"
CT="Content-Type: application/json"

# ---- Step 3: Check realm ----
echo "🏗️   Checking realm '${REALM}'..."
REALM_EXISTS=$(curl -s \
    --connect-timeout 10 \
    --max-time 30 \
    -H "$AUTH" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}" 2>/dev/null \
    | grep -c '"realm"' || true)

if [ "$REALM_EXISTS" -gt 0 ]; then
    echo "ℹ️   Realm '${REALM}' already exists — skipping creation."
else
    curl -s \
        --connect-timeout 10 \
        --max-time 30 \
        -X POST \
        -H "$AUTH" -H "$CT" \
        -d "{
            \"realm\": \"${REALM}\",
            \"enabled\": true,
            \"displayName\": \"OrderPay\",
            \"sslRequired\": \"none\",
            \"registrationAllowed\": true,
            \"registrationEmailAsUsername\": true,
            \"loginWithEmailAllowed\": true,
            \"duplicateEmailsAllowed\": false,
            \"resetPasswordAllowed\": true,
            \"verifyEmail\": false,
            \"bruteForceProtected\": true,
            \"defaultRoles\": [\"customer\"]
        }" \
        "${KEYCLOAK_URL}/admin/realms"
    echo "✅  Realm '${REALM}' created."
fi
echo ""

# ---- Step 4: Create roles ----
echo "🔐  Creating roles..."
for ROLE in admin customer; do

    ROLE_EXISTS=$(curl -s \
        -H "$AUTH" \
        "${KEYCLOAK_URL}/admin/realms/${REALM}/roles/${ROLE}" \
        2>/dev/null || true)

    if echo "$ROLE_EXISTS" | grep -q "\"name\":\"${ROLE}\""; then
        echo "ℹ️   Role '${ROLE}' already exists."
    else
        curl -sf -X POST \
            -H "$AUTH" -H "$CT" \
            -d "{\"name\":\"${ROLE}\",\"description\":\"${ROLE} role\"}" \
            "${KEYCLOAK_URL}/admin/realms/${REALM}/roles"

        echo "✅  Role '${ROLE}' created."
    fi
done
echo ""

# ---- Step 5: Create clients ----
echo "🔧  Creating clients..."
echo ""

# ------------------------------------------------------------
# orderpay-webapi
# ------------------------------------------------------------
echo "➡️  Checking client 'orderpay-webapi'..."

CLIENT_EXISTS=$(curl -s \
    --connect-timeout 10 \
    --max-time 30 \
    -H "$AUTH" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/clients?clientId=orderpay-webapi")

if echo "$CLIENT_EXISTS" | jq -e 'length > 0' > /dev/null; then

    echo "ℹ️   Client 'orderpay-webapi' already exists."

else

    echo "➕  Creating client 'orderpay-webapi'..."

    RESULT=$(curl -s \
        --connect-timeout 10 \
        --max-time 30 \
        -o /tmp/client-response.txt \
        -w "%{http_code}" \
        -X POST \
        -H "$AUTH" \
        -H "$CT" \
        -d '{
            "clientId": "orderpay-webapi",
            "name": "OrderPay WebApi",
            "enabled": true,
            "publicClient": false,
            "bearerOnly": true,
            "standardFlowEnabled": false,
            "protocol": "openid-connect"
        }' \
        "${KEYCLOAK_URL}/admin/realms/${REALM}/clients")

    if [ "$RESULT" = "201" ]; then

        echo "✅  Client 'orderpay-webapi' created."

    else

        echo "❌  Failed to create client 'orderpay-webapi'"
        echo ""
        echo "HTTP STATUS: $RESULT"
        echo "RESPONSE:"
        cat /tmp/client-response.txt
        exit 1

    fi
fi

echo ""

# ------------------------------------------------------------
# orderpay-swagger
# ------------------------------------------------------------
echo "➡️  Checking client 'orderpay-swagger'..."

CLIENT_EXISTS=$(curl -s \
    --connect-timeout 10 \
    --max-time 30 \
    -H "$AUTH" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/clients?clientId=orderpay-swagger")

if echo "$CLIENT_EXISTS" | jq -e 'length > 0' > /dev/null; then

    echo "ℹ️   Client 'orderpay-swagger' already exists."

else

    echo "➕  Creating client 'orderpay-swagger'..."

    RESULT=$(curl -s \
        --connect-timeout 10 \
        --max-time 30 \
        -o /tmp/swagger-client-response.txt \
        -w "%{http_code}" \
        -X POST \
        -H "$AUTH" \
        -H "$CT" \
        -d '{
            "clientId": "orderpay-swagger",
            "name": "OrderPay Swagger UI",
            "enabled": true,
            "publicClient": true,
            "bearerOnly": false,
            "standardFlowEnabled": true,
            "directAccessGrantsEnabled": true,
            "protocol": "openid-connect",
            "redirectUris": [
                "http://api.localhost/*",
                "http://www.localhost/*",
                "http://localhost/*",
                "http://localhost:8080/*",
                "http://localhost:5208/*",
                "http://127.0.0.1/*"
            ],
            "webOrigins": [
                "http://api.localhost",
                "http://www.localhost",
                "http://localhost",
                "http://localhost:8080",
                "http://localhost:5208",
                "http://127.0.0.1"
            ]
        }' \
        "${KEYCLOAK_URL}/admin/realms/${REALM}/clients")

    if [ "$RESULT" = "201" ]; then

        echo "✅  Client 'orderpay-swagger' created."

    else

        echo "❌  Failed to create client 'orderpay-swagger'"
        echo ""
        echo "HTTP STATUS: $RESULT"
        echo "RESPONSE:"
        cat /tmp/swagger-client-response.txt
        exit 1

    fi
fi

echo ""

# ---- Helper: create user and assign roles ----
create_user() {
    EMAIL=$1
    PASSWORD=$2
    FIRSTNAME=$3
    LASTNAME=$4
    ROLES=$5    # comma-separated: "admin,customer"

    echo "👤  Creating user '${EMAIL}'..."

    RESULT=$(curl -s \
        --connect-timeout 10 \
        --max-time 30 \
        -o /dev/null \
        -w "%{http_code}" \
        -X POST \
        -H "$AUTH" -H "$CT" \
        -d "{
            \"username\": \"${EMAIL}\",
            \"email\": \"${EMAIL}\",
            \"enabled\": true,
            \"emailVerified\": true,
            \"firstName\": \"${FIRSTNAME}\",
            \"lastName\": \"${LASTNAME}\",
            \"credentials\": [{
                \"type\": \"password\",
                \"value\": \"${PASSWORD}\",
                \"temporary\": false
            }]
        }" \
        "${KEYCLOAK_URL}/admin/realms/${REALM}/users")

    if [ "$RESULT" != "201" ]; then
        echo "ℹ️   User '${EMAIL}' already exists."
        return
    fi

    # get user ID
    USER_ID=$(curl -s \
        --connect-timeout 10 \
        --max-time 30 \
        -H "$AUTH" \
        "${KEYCLOAK_URL}/admin/realms/${REALM}/users?username=${EMAIL}" \
        | jq -r '.[0].id')

    # assign roles
    ROLE_ARRAY="["
    for ROLE in $(echo "$ROLES" | tr ',' ' '); do
        ROLE_DATA=$(curl -sf \
            --connect-timeout 10 \
            --max-time 30 \
            -H "$AUTH" \
            "${KEYCLOAK_URL}/admin/realms/${REALM}/roles/${ROLE}")
        ROLE_ID=$(echo "$ROLE_DATA" \
            | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
        ROLE_ARRAY="${ROLE_ARRAY}{\"id\":\"${ROLE_ID}\",\"name\":\"${ROLE}\"},"
    done
    ROLE_ARRAY="${ROLE_ARRAY%,}]"

    curl -s \
        --connect-timeout 10 \
        --max-time 30 \
        -X POST \
        -H "$AUTH" -H "$CT" \
        -d "${ROLE_ARRAY}" \
        "${KEYCLOAK_URL}/admin/realms/${REALM}/users/${USER_ID}/role-mappings/realm" \
        > /dev/null 2>&1

    echo "✅  User '${EMAIL}' created with roles: ${ROLES}"
}

# ---- Step 6: orderpay-web client (Next.js frontend — confidential) ----
echo "➡️  Checking client 'orderpay-web'..."

CLIENT_EXISTS=$(curl -s \
    --connect-timeout 10 \
    --max-time 30 \
    -H "$AUTH" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/clients?clientId=orderpay-web")

if echo "$CLIENT_EXISTS" | jq -e 'length > 0' > /dev/null; then

    echo "ℹ️   Client 'orderpay-web' already exists."

else

    echo "➕  Creating client 'orderpay-web'..."
    WEB_SECRET="${KEYCLOAK_WEB_CLIENT_SECRET:-orderpay-web-dev-secret}"

    RESULT=$(curl -s \
        --connect-timeout 10 \
        --max-time 30 \
        -o /tmp/web-client-response.txt \
        -w "%{http_code}" \
        -X POST \
        -H "$AUTH" \
        -H "$CT" \
        -d "{
            \"clientId\": \"orderpay-web\",
            \"name\": \"OrderPay Web Frontend\",
            \"enabled\": true,
            \"publicClient\": false,
            \"bearerOnly\": false,
            \"standardFlowEnabled\": true,
            \"directAccessGrantsEnabled\": false,
            \"protocol\": \"openid-connect\",
            \"secret\": \"${WEB_SECRET}\",
            \"redirectUris\": [
                \"http://www.localhost/api/auth/callback/keycloak\",
                \"http://localhost/api/auth/callback/keycloak\",
                \"http://localhost:3000/api/auth/callback/keycloak\"
            ],
            \"webOrigins\": [
                \"http://www.localhost\",
                \"http://localhost\",
                \"http://localhost:3000\"
            ]
        }" \
        "${KEYCLOAK_URL}/admin/realms/${REALM}/clients")

    if [ "$RESULT" = "201" ]; then

        echo "✅  Client 'orderpay-web' created."

    else

        echo "❌  Failed to create client 'orderpay-web'"
        echo ""
        echo "HTTP STATUS: $RESULT"
        echo "RESPONSE:"
        cat /tmp/web-client-response.txt
        exit 1

    fi
fi

echo ""

# ---- Step 7: Audience mapper — orderpay-swagger → orderpay-webapi ----
echo "🗺️   Configuring audience mapper for 'orderpay-swagger'..."

SWAGGER_UUID=$(curl -s \
    --connect-timeout 10 \
    --max-time 30 \
    -H "$AUTH" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/clients?clientId=orderpay-swagger" \
    | jq -r '.[0].id')

MAPPER_EXISTS=$(curl -s \
    --connect-timeout 10 \
    --max-time 30 \
    -H "$AUTH" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/clients/${SWAGGER_UUID}/protocol-mappers/models" \
    | jq '[.[] | select(.name == "orderpay-webapi-audience")] | length')

if [ "$MAPPER_EXISTS" -gt 0 ]; then
    echo "ℹ️   Audience mapper already exists."
else
    curl -sf -X POST \
        -H "$AUTH" -H "$CT" \
        -d '{
            "name": "orderpay-webapi-audience",
            "protocol": "openid-connect",
            "protocolMapper": "oidc-audience-mapper",
            "consentRequired": false,
            "config": {
                "included.client.audience": "orderpay-webapi",
                "id.token.claim": "false",
                "access.token.claim": "true"
            }
        }' \
        "${KEYCLOAK_URL}/admin/realms/${REALM}/clients/${SWAGGER_UUID}/protocol-mappers/models"
    echo "✅  Audience mapper added."
fi
echo ""

# ---- Step 8: Create users ----
echo "👥  Creating users..."
echo ""
create_user "admin@orderpay.com" "Mauri@22" "Admin" "OrderPay" "admin,customer"
create_user "user@orderpay.com"  "User@123"  "User"  "OrderPay" "customer"
echo ""

# ---- Done ----
echo "════════════════════════════════════════"
echo "✅  Keycloak setup complete!"
echo "════════════════════════════════════════"
echo ""
echo "  🌐 Admin Console:  ${KEYCLOAK_URL}/admin"
echo "  🔐 Realm:          ${REALM}"
echo "  📝 Register:       ${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/registrations"
echo "  🔑 Login:          ${KEYCLOAK_URL}/realms/${REALM}/account"
echo ""
echo "  👤 Admin:          admin@orderpay.com / Mauri@22"
echo "  👤 Customer:       user@orderpay.com  / User@123"
echo ""
echo "  🆕 New users can self-register and auto-receive 'customer' role"
echo "════════════════════════════════════════"
echo ""