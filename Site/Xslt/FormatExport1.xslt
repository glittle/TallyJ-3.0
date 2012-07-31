<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output media-type="html" indent="yes" omit-xml-declaration="yes" />
    <xsl:template match="/">
        <html>
            <head>
                <title>Exported Election</title>
            </head>
            <body>
                <h1>
                    <xsl:value-of select="election/@Name"/>
                </h1>
                <h2>Results</h2>
                <table>
                    <tr>
                        <td>Rank</td>
                        <td>Name</td>
                        <td>Votes</td>
                    </tr>
                    <xsl:apply-templates select="//result[@Section='T' or @Section='E']"/>
                </table>
            </body>
        </html>
    </xsl:template>
    <xsl:template match="result">
        <xsl:variable name="result" select ="."/>
        <xsl:variable name="person" select="//person[@PersonGuid=$result/@PersonGuid]"/>
        <tr>
            <td>

                <xsl:value-of select="@Rank"/>
            </td>
            <td>
                <xsl:value-of select="$person/@FirstName"/>
                <xsl:text> </xsl:text>
                <xsl:value-of select="$person/@LastName"/>
            </td>
            <td>
                <xsl:value-of select="@VoteCount"/>
                <xsl:choose>
                    <xsl:when test="@TieBreakCount != 0">
                        <xsl:text>, </xsl:text>
                        <xsl:value-of select="@TieBreakCount"/>
                    </xsl:when>
                </xsl:choose>
            </td>
        </tr>
    </xsl:template>
</xsl:stylesheet>
