# Spendwise Web

React + TypeScript web client for the Spendwise ASP.NET Core API.

## Run locally

1. Start the API using the HTTPS profile (`https://localhost:7195`).
2. Copy `.env.example` to `.env.local` and set `VITE_API_BASE_URL` if the API uses a different URL.
3. From this directory, run `npm install` and `npm run dev`.

The client includes sign-in, business and cashbook selection, a transaction overview, and the receipt upload → OCR → suggestion → review → confirm flow. Auth tokens are kept in `sessionStorage` for the current browser session.
