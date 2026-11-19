INSERT INTO cases (id, title) 
    VALUES 
    ('case-001', 'Repeated login failures'), 
    ('case-no-activity', 'A case without activity');

INSERT INTO case_activity (id, case_id, message) 
    VALUES 
    ('act-001', 'case-001', 'Analyst reviewed the evidence'), 
    ('act-002', 'case-001', 'Owner was notified');