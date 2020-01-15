$(function () {
    //Click en los enlaces a los trÃ¡mites
    $('#tramites-portada ul li a').click(function () {
        var titulo = $(this).text();

        ga('send', 'event', 'tramites-portada', 'click', titulo, 1);
    });

    //Click en los enlaces destacados del menÃº superior
    $('#menu_sup li a').click(function () {
        var titulo = $(this).attr('title');

        ga('send', 'event', 'menu-header', 'click', titulo, 1);
    });

    //Click en los banners de la derecha
    $('.destacados .media-heading a').click(function () {
        var titulo = $(this).text();

        ga('send', 'event', 'banner-derecha', 'click', titulo, 1);
    });

    $('.destacados a.pull-left').click(function () {
        var titulo = $('img', this).attr('alt');

        ga('send', 'event', 'banner-derecha', 'click', titulo, 1);
    });
});