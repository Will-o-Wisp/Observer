function SetupArticle(){
    document.getElementById("ArticleHeader").innerHTML = Config.ArticleHeader;
    document.getElementById("GitHubRedirection").setAttribute("href", Config.GitRepository);
}