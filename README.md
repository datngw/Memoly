# Memoly

A keeper of memories — the people, the trips, the things, and the stories they carry.

## Overview

Time takes things quietly. The trip you swore you'd never forget. The voice you keep meaning to record. The old objects in your drawer, each with a story no one has written down.

**Memoly is a home for those memories.** Save a photo, a video, a voice recording, or your own words — and keep the feeling with them: who was there, where it happened, why it mattered. Not files in a folder, but memories you can walk through whenever you miss them.

It holds the moments life is made of — dinners with the people you love, journeys that changed you, the little things you keep because of what they've seen, a voice you'll want to hear again years from now. Organize them into collections and find any of them again in seconds, or share one as a link — a little page of its own, styled your way.

And someday you'll want to look back — a quiet evening, a song that reminds you of someone. Open Memoly, and the beautiful moments are all there, waiting.

Everything can be backed up and taken with you. Private by design — no feeds, no tracking, just your memories and the people in them.

## Why I Built This

Because I kept meeting memories I could no longer reach. A song plays and I can't remember the trip. There's a plant someone once gave me years ago. A voice I'd give anything to hear again was never recorded.

I didn't want a social network, and I didn't want another cloud folder. I wanted one quiet place where a memory keeps its whole self — the photo and the feeling, the voice and the story — and where everything stays mine: private, exportable, built to be reopened years from now, not scrolled past today.

## Features

- **Four kinds of entries** — photos, videos, voice recordings, and writing
- **The details that matter** — a mood, a place, and the story, saved with each entry
- **Collections** — group entries around a trip, a year, a person
- **Search** — find any old entry in seconds
- **Share links** — publish a memory as a page with its own colors and text; revoke it anytime
- **Backup & transfer** — export and import everything, any time

## Architecture

**Vertical Slice Architecture** — a single .NET application organized by feature slices (endpoint, handler, validator, EF Core configuration) over a shared `MemolyDbContext`, with Wolverine as the in-process command bus.

![Memoly system architecture](docs/architecture.png?v=2)

## Tech Stack

### Backend

| Component      | Technology                                           |
| -------------- | ---------------------------------------------------- |
| Runtime        | .NET 10, C# 14                                       |
| Orchestration  | .NET Aspire                                          |
| API            | ASP.NET Core Minimal APIs                            |
| ORM            | EF Core                                              |
| API Docs       | Scalar                                               |
| Logging        | Serilog + Seq                                        |
| Resilience     | Polly v8                                             |
| Messaging & CQRS | Wolverine (in-process command bus, EF Core durable messaging) |
| Validation     | FluentValidation                                     |
| Mapping        | Mapster                                              |
| Authentication | Keycloak (OIDC)                                      |
| Testing        | xUnit v3, Fluent Assertions, Moq, Testcontainers    |
| Code Quality   | SonarQube                                            |
| Architecture   | Vertical Slice Architecture                          |

### Frontend

| Component     | Technology          |
| ------------- | ------------------- |
| Framework     | React + Vite        |
| State         | Zustand             |
| UI Components | shadcn/ui           |
| Icons         | Lucide Icons        |
| Styling       | Tailwind CSS        |

## Getting Started

> The project has not been scaffolded yet.

```bash
# Backend (via Aspire — starts API + all containers)
dotnet run --project src/Aspire/Memoly.AppHost

# Frontend
cd src/Frontend/memoly-web
npm install
npm run dev
```

## License

Personal project — not for distribution.
