# Funcy

Funcy is a terminal-based (TUI) tool for monitoring and administering **Azure Function Apps** across Azure subscriptions.

It is built with **Spectre.Console** and inspired by tools like **btop** and **k9s**, focusing on fast feedback, keyboard-driven workflows, and minimal friction.

---

## Features

- List Azure Function Apps in the active subscription
- Fast startup using a local cache, followed by background refresh from Azure
- Start and stop Function Apps
- Swap deployment slots to production
- Switch Azure subscriptions at runtime
- Filter and sort large lists efficiently
- Fully keyboard-driven UI

---

## Requirements

### Runtime

- **.NET 9**
- **Azure CLI (`az`)**

### Azure CLI extensions

Funcy depends on **Azure Resource Graph**, which is **not installed by default**.

Install it explicitly:

```bash
az extension add --name resource-graph
```

Verify installation:

```bash
az extension list
```

### Azure authentication

Funcy uses `DefaultAzureCredential`.

You must be logged in via Azure CLI:

```bash
az login
```

Required permissions:

- **Read** access to list Function Apps (Resource Graph)
- **Contributor** (or equivalent) to start/stop apps or swap slots

---

## Getting started

1. Log in to Azure:
   ```bash
   az login
   ```

2. (Optional) Set a default subscription:
   ```bash
   az account set --subscription "<subscription name or id>"
   ```

3. Build and run:
   ```bash
   dotnet run --project src/Funcy.Console
   ```

On startup, Funcy:
1. Loads Function Apps from a local database cache (fast)
2. Refreshes data from Azure in the background

---

## Subscription switching

Funcy supports **switching Azure subscriptions at runtime**.

- The currently active subscription is shown in the top panel
- Use the subscription shortcut to open the **Switch Subscription** view
- Selecting a new subscription:
  - Updates the global application context
  - Clears cached Function Apps
  - Reloads data for the new subscription (cache → Azure)

After switching, you always return to the Function Apps view.

---

## Keyboard shortcuts

### Global

- **F** – Filter
- **R** – Refresh
- **S** – Start Function App
- **T** – Stop Function App
- **W** – Swap slot to production
- **Enter** – Navigate into selection
- **Esc / Space** – Go back
- **Delete** – Clear filter
- **↑ / ↓ / PgUp / PgDn** – Scroll
- **1..n** – Sort by column
- **U** – Open subscription switcher

---

## Notes & limitations

- Azure Resource Graph **must** be installed, or no Function Apps will be listed
- Visible subscriptions depend on the active Azure CLI account and tenant

---

## Roadmap (informal)

- Settings view
- Favorites / pinned Function Apps
- Hide functionality for subscriptions
- Improved error surfacing in UI
- Throttle refresh on subscription change (max once every 5 minutes)
- View Service Bus message count
