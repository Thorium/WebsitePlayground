USE companyweb;

DELETE FROM company WHERE ID IN (1,2,3);

-- People --
INSERT INTO company(Id, Name, CEO, Founded, Founder, WebSite, LogoUrl, LastUpdate)
    VALUES (1, 'Microsoft', 'Satya Nadella', '1975-04-04', 'Bill Gates and Paul Allen', 'http://www.microsoft.com', null, CURDATE());
INSERT INTO company(Id, Name, CEO, Founded, Founder, WebSite, LogoUrl, LastUpdate)
    VALUES (2, 'Google', 'Larry Page', '1998-09-04', 'Larry Page and Sergey Brin', 'http://www.google.com', 'https://upload.wikimedia.org/wikipedia/commons/thumb/3/30/Googlelogo.png/800px-Googlelogo.png', CURDATE());
INSERT INTO company(Id, Name, CEO, Founded, Founder, WebSite, LogoUrl, LastUpdate)
    VALUES (3, 'My New Company', 'Me', CURDATE(), 'Me', null, null, CURDATE());
