<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="2.0" 
                xmlns:html="http://www.w3.org/TR/REC-html40"
                xmlns:sitemap="http://www.sitemaps.org/schemas/sitemap/0.9"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="html" version="1.0" encoding="UTF-8" indent="yes"/>
	<xsl:template match="/">
		<html xmlns="http://www.w3.org/1999/xhtml">
			<head>
				<meta http-equiv="Content-Type" content="text/html; charset=UTF-8" />
				<meta name="viewport" content="width=device-width, initial-scale=1.0" />
				<title>Sitemap</title>
                <link rel='stylesheet' type='text/css' href='css/libs.min.css' /> 
                <link rel='stylesheet' type='text/css' href='css/app.min.css' /> 
                <script type='text/javascript' src='js/libs.min.js' defer='defer'></script>
                <script type='text/javascript' src='/signalr/hubs' defer='defer'></script> <!-- Generated SignalR Hub! -->
                <script type='text/javascript' src='js/app.min.js' defer='defer'></script>
			</head>
			<body>
				<div class="pageLoader"><nav class="tab-bar desktop-navbar desktop-navbar-normal"></nav><span class="fa fa-spinner fa-spin fa-4x"></span></div>
				<div class="pageLoaded">

					<div class="bgImg">        
						<div id="navbar" class="desktop-navbar-hero"></div>
						<div style="height:0.5rem;"></div>
						<div class="row full-width">
							<div class="small-9 medium-9 large-9 columns">
								<h1>Sitemap, Links</h1>
							</div>
							<div class="medium-3 large-3 columns"></div>
						</div>
						<div style="height:3em;"></div>
					</div>
					<div class="row">
						<br/><br/>
						<div class="small-12 columns">
							<div style="height:8rem;"></div>
							<table cellpadding="5">
								<tr style="border-bottom:1px black solid;">
									<th>URL</th>
									<th>Priority</th>
									<th>Change Frequency</th>
									<th>LastChange (GMT)</th>
								</tr>
								<xsl:variable name="lower" select="'abcdefghijklmnopqrstuvwxyz'"/>
								<xsl:variable name="upper" select="'ABCDEFGHIJKLMNOPQRSTUVWXYZ'"/>
								<xsl:for-each select="sitemap:urlset/sitemap:url">
									<tr>
										<xsl:if test="position() mod 2 != 1">
											<xsl:attribute  name="class">high</xsl:attribute>
										</xsl:if>
										<td>
											<xsl:variable name="itemURL">
												<xsl:value-of select="sitemap:loc"/>
											</xsl:variable>
											<a href="{$itemURL}">
												<xsl:value-of select="sitemap:loc"/>
											</a>
										</td>
										<td>
											<xsl:value-of select="concat(sitemap:priority*100,'%')"/>
										</td>
										<td>
											<xsl:value-of select="concat(translate(substring(sitemap:changefreq, 1, 1),concat($lower, $upper),concat($upper, $lower)),substring(sitemap:changefreq, 2))"/>
										</td>
										<td>
											<xsl:value-of select="concat(substring(sitemap:lastmod,0,11),concat(' ', substring(sitemap:lastmod,12,5)))"/>
										</td>
									</tr>
								</xsl:for-each>
							</table>
						</div>
					</div>
				<div id="footer"></div>
				</div>
			</body>
		</html>

	</xsl:template>

</xsl:stylesheet>			