<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="UTF-8" indent="yes"/>

  <xsl:template match="/">
    <html>
      <head>
        <title>List of Teachers</title>
        <style>
          body { font-family: sans-serif; }
          table { width: 100%; border-collapse: collapse; }
          th, td { border: 1px solid #ddd; padding: 8px; text-align: left; vertical-align: top; }
          th { background-color: #f2f2f2; }
          ul { margin: 0; padding-left: 20px; }
        </style>
      </head>
      <body>
        <h1>List of Teachers</h1>
        <table>
          <thead>
            <tr>
              <th>Full Name</th>
              <th>Faculty</th>
              <th>Department/Branch</th>
              <th>Position</th>
              <th>Education</th>
            </tr>
          </thead>
          <tbody>
            <xsl:for-each select="Teachers/Teacher">
              <tr>
                <td><xsl:value-of select="@FullName"/></td>
                <td><xsl:value-of select="Faculty"/></td>
                <td><xsl:value-of select="Department"/></td>
                <td><xsl:value-of select="Position"/></td>
                <td>
                  <ul>
                    <xsl:for-each select="Educations/Education">
                      <li>
                        <b><xsl:value-of select="@Level"/>:</b>
                        <xsl:value-of select="Institution"/>
                        <i>(<xsl:value-of select="Period"/>)</i>
                      </li>
                    </xsl:for-each>
                  </ul>
                </td>
              </tr>
            </xsl:for-each>
          </tbody>
        </table>
      </body>
    </html>
  </xsl:template>

</xsl:stylesheet>