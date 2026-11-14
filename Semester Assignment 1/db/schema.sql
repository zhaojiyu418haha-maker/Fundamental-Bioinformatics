-- Battlegrounds Gallery — PostgreSQL schema
-- Run with:
--   psql -h 127.0.0.1 -U bg_app -d battlegrounds_gallery -f db/schema.sql

BEGIN;

CREATE TABLE IF NOT EXISTS tribes (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(32)  NOT NULL UNIQUE,
    description TEXT         NOT NULL DEFAULT '',
    color       VARCHAR(7)   NOT NULL DEFAULT '#8a6a3f'
);

CREATE TABLE IF NOT EXISTS cards (
    id          SERIAL PRIMARY KEY,
    slug        VARCHAR(120) NOT NULL UNIQUE,
    name        VARCHAR(100) NOT NULL,
    description TEXT         NOT NULL DEFAULT '',
    attack      INTEGER      NOT NULL CHECK (attack >= 0),
    health      INTEGER      NOT NULL CHECK (health >= 0),
    tier        INTEGER      NOT NULL CHECK (tier BETWEEN 1 AND 7),
    image_path  TEXT         NOT NULL,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS cards_tribes (
    card_id    INTEGER NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    tribe_id   INTEGER NOT NULL REFERENCES tribes(id) ON DELETE RESTRICT,
    is_primary BOOLEAN NOT NULL DEFAULT FALSE,
    PRIMARY KEY (card_id, tribe_id)
);

CREATE TABLE IF NOT EXISTS users (
    id            SERIAL PRIMARY KEY,
    username      VARCHAR(32)  NOT NULL UNIQUE,
    email         VARCHAR(128) NOT NULL UNIQUE,
    password_hash TEXT         NOT NULL,
    role          VARCHAR(16)  NOT NULL DEFAULT 'user'
                  CHECK (role IN ('user', 'admin')),
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS feedback (
    id         SERIAL PRIMARY KEY,
    name       VARCHAR(64)  NOT NULL,
    email      VARCHAR(128) NOT NULL,
    message    TEXT         NOT NULL,
    created_at TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS cards_tier_idx ON cards(tier);
CREATE INDEX IF NOT EXISTS cards_name_lower_idx ON cards(LOWER(name));
CREATE INDEX IF NOT EXISTS cards_tribes_tribe_idx ON cards_tribes(tribe_id);

COMMIT;
