import os
import Auxiliary

FinalArticlePath = Auxiliary.RelativePath("../html/articlebody.html")
FinalResources = Auxiliary.RelativePath("../resources")
FinalConfig = Auxiliary.RelativePath("../js/config.js")
intermediatefile = "intermediate.html"

#----------------------------------------------------------------------------------

os.system("python -m markdown articlebody.md > " + intermediatefile)

alltext = Auxiliary.ReadAllText(intermediatefile)
alltext = alltext.replace("<pre><code>", "<div class='codebox'><pre><code>")
alltext = alltext.replace("</code></pre>", "</code></pre></div>")

Auxiliary.WriteAllText(FinalArticlePath, alltext)

Auxiliary.DeleteFile(intermediatefile)

Auxiliary.DeleteDirectory(FinalResources)
Auxiliary.CopyDirectory("resources", FinalResources)

Auxiliary.CopyFile("config.js", FinalConfig)