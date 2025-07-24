// wwwroot/js/footer-effects.js
$(document).ready(function () {

    // Tech badge hover efektleri
    $('.tech-badge').hover(
        function () {
            $(this).addClass('shadow-sm');
        },
        function () {
            $(this).removeClass('shadow-sm');
        }
    );

    // Footer logo'ya tıklanınca kahve fincanı animasyonu
    $('.footer-logo').click(function () {
        $(this).addClass('fa-spin');
        setTimeout(() => {
            $(this).removeClass('fa-spin');
        }, 1000);

        // Easter egg - kahve mesajı
        ToastHelper.info('☕ Kahve molası zamanı! ☕');
    });

    // İmza tıklama efekti
    $('.dev-signature').click(function () {
        $(this).fadeOut(200).fadeIn(200).fadeOut(200).fadeIn(200);
        ToastHelper.success('👨‍💻 Geliştirici ekibi selamlar! 🚀');
    });


});