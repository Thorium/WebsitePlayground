USE CompanyWeb

DELETE FROM Company WHERE ID IN (1,2,3);

-- People --
INSERT INTO Company(Name, CEO, Founded, Founder, WebSite, LogoUrl, LastUpdate)
    VALUES ('Microsoft', 'Satya Nadella', '1975-04-04', 'Bill Gates and Paul Allen', 'http://www.microsoft.com', null, getDate());
INSERT INTO Company(Name, CEO, Founded, Founder, WebSite, LogoUrl, LastUpdate)
    VALUES ('Google', 'Larry Page', '1998-09-04', 'Larry Page and Sergey Brin', 'http://www.google.com', 'https://upload.wikimedia.org/wikipedia/commons/thumb/3/30/Googlelogo.png/800px-Googlelogo.png', getDate());
INSERT INTO Company(Name, CEO, Founded, Founder, WebSite, LogoUrl, LastUpdate)
    VALUES ('My New Company', 'Me', getDate(), 'Me', null, null, getDate());
