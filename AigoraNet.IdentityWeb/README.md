# AIGORA Frontend - Backend Requirements

This document outlines the backend features required by the frontend that were not explicitly found or fully clear in the provided API specification.

## Authentication
- **Login Endpoint**: The frontend assumes `POST /auth/login` exists for user login.
    - **Payload**: `{ email, password }`
    - **Response**: `{ token: string, user: object }`
    - *Current Implementation*: Uses `POST /auth/login` (placeholder). If the actual endpoint is different (e.g., `/auth/tokens`), the frontend `src/views/LoginView.vue` needs to be updated.

## Key Management
- **Keyword Prompt Management**: The frontend uses `/system/keyword-prompts` for managing user keys.
    - **Create**: `POST /system/keyword-prompts`
        - Assumed Payload: `{ keyword, prompt }`
    - **List**: `GET /system/keyword-prompts`
    - **Delete**: `DELETE /system/keyword-prompts/{id}`
    - *Note*: The OpenAPI spec mentions `UpsertKeywordPromptCommand` but detailed properties were inferred. Please ensure the backend supports these fields.

## User Profile
- **Get Current User**: The frontend assumes a way to get the current user's info, potentially `GET /private/members/me` (found in spec).
    - *Current Implementation*: Stores user info in local state after login.

## CORS
- Ensure CORS is enabled for the frontend domain (e.g., `http://localhost:5173` for dev, `https://aigora.net` for prod).
