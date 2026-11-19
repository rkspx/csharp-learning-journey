CREATE TABLE cases (
    id TEXT NOT NULL PRIMARY KEY,
    title TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'New',
    CONSTRAINT cases_title_not_blank CHECK (length(trim(title)) > 0),
    CONSTRAINT cases_status_allowed CHECK (status IN('New', 'Investigating', 'ClosureRequested', 'Closed'))
);