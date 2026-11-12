<?xml version="1.0"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <!-- A template -->
  <xsl:template match="/">
    <xsl:for-each select="items/item">
      <li><xsl:value-of select="@name"/></li>
    </xsl:for-each>
  </xsl:template>
</xsl:stylesheet>
