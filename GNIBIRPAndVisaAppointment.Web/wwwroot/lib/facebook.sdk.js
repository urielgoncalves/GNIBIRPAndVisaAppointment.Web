window.fbAsyncInit = function() {
    FB.init({
        appId: '1471497129644152',
        autoLogAppEvents: true,
        xfbml: true,
        version: 'v3.2'
    });

    FB.AppEvents.logPageView();

    FB.getLoginStatus(function(response) {
        var loginButtons = document.getElementsByClassName("facebook-login");
        for (var index in loginButtons) {
            var loginButton = loginButtons[index];
            loginButton.parentElement.remove(loginButton);
        }
    });
};

(function(d, s, id) {
    var js, fjs = d.getElementsByTagName(s)[0];
    if (d.getElementById(id)) return;
    js = d.createElement(s); js.id = id;
    js.src = 'https://connect.facebook.net/en_US/sdk/xfbml.customerchat.js';
    fjs.parentNode.insertBefore(js, fjs);
}(document, 'script', 'facebook-jssdk'));