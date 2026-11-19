CREATE TABLE cases(
    id TEXT NOT NULL PRIMARY KEY,
    title TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'New',
    CONSTRAINT cases_title_not_blank CHECK (length(trim(title)) > 0),
    CONSTRAINT cases_status_allowed CHECK (status IN ('New', 'Investigating', 'ClosureRequested', 'Closed'))
);

CREATE TABLE case_activity(
    id TEXT NOT NULL PRIMARY KEY,
    case_id TEXT NOT NULL,
    message TEXT NOT NULL,
    CONSTRAINT activity_message_not_blank CHECK(length(trim(message)) > 0),
    CONSTRAINT activity_case_exists FOREIGN KEY (case_id) REFERENCES cases(id)
);