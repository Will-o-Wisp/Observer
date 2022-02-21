
async function MakeAjaxRequest(element, file){
    xhttp = new XMLHttpRequest();
    xhttp.onreadystatechange = function() {
        if (this.readyState == 4) {
            if (this.status == 200) {element.innerHTML = this.responseText;}
            if (this.status == 404) {element.innerHTML = "Page not found.";}
            element.removeAttribute("IncludeHtml");
        }
    }

    xhttp.open("GET", file, false);
    xhttp.send();
}

async function IncludeHTML(){
    var allElements = document.getElementsByTagName("*");
    var allPromises = [];

    for (var i = 0; i < allElements.length; i++) {
        var element = allElements[i];
        var file = element.getAttribute("IncludeHtml");
        if(file == null) continue;

        allPromises.push(MakeAjaxRequest(element, file));
    }

    await Promise.all(allPromises);
}