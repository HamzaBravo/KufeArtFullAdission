// wwwroot/js/profile-manager.js
window.ProfileManager = {

    // Profil modalını aç
    openProfileModal: function () {
        const profileModalHtml = `
            <div class="modal fade" id="profileModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header bg-primary text-white">
                            <h5 class="modal-title">
                                <i class="fas fa-user-circle me-2"></i>Profil Bilgileri
                            </h5>
                            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="text-center mb-4">
                                <div class="profile-avatar-large mx-auto mb-3" style="width: 80px; height: 80px;">
                                    <i class="fas fa-user text-white fs-1"></i>
                                </div>
                                <h4>${getCurrentUserName()}</h4>
                                <p class="text-muted">${getCurrentUsername()}</p>
                            </div>
                            
                            <div class="row">
                                <div class="col-6">
                                    <div class="card bg-light">
                                        <div class="card-body text-center">
                                            <i class="fas fa-calendar text-primary fs-4"></i>
                                            <h6 class="mt-2">Üyelik</h6>
                                            <small>Admin</small>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-6">
                                    <div class="card bg-light">
                                        <div class="card-body text-center">
                                            <i class="fas fa-clock text-success fs-4"></i>
                                            <h6 class="mt-2">Son Giriş</h6>
                                            <small>Şimdi</small>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Kapat</button>
                            <button type="button" class="btn btn-primary" onclick="ProfileManager.openChangePasswordModal(); $('#profileModal').modal('hide');">
                                <i class="fas fa-key me-2"></i>Şifre Değiştir
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        $('body').append(profileModalHtml);
        $('#profileModal').modal('show');

        // Modal kapatıldığında temizle
        $('#profileModal').on('hidden.bs.modal', function () {
            $(this).remove();
        });
    },

    // Şifre değiştirme modalını aç
    openChangePasswordModal: function () {
        const passwordModalHtml = `
            <div class="modal fade" id="changePasswordModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header bg-warning text-dark">
                            <h5 class="modal-title">
                                <i class="fas fa-key me-2"></i>Şifre Değiştir
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <form id="changePasswordForm">
                                <div class="mb-3">
                                    <label class="form-label">Mevcut Şifre</label>
                                    <input type="password" id="currentPassword" class="form-control" 
                                           placeholder="Mevcut şifrenizi girin" required autocomplete="off">
                                </div>
                                <div class="mb-3">
                                    <label class="form-label">Yeni Şifre</label>
                                    <input type="password" id="newPassword" class="form-control" 
                                           placeholder="Yeni şifrenizi girin" required autocomplete="off">
                                </div>
                                <div class="mb-3">
                                    <label class="form-label">Yeni Şifre (Tekrar)</label>
                                    <input type="password" id="confirmPassword" class="form-control" 
                                           placeholder="Yeni şifrenizi tekrar girin" required autocomplete="off">
                                </div>
                            </form>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">İptal</button>
                            <button type="button" class="btn btn-warning" onclick="ProfileManager.changePassword()">
                                <i class="fas fa-save me-2"></i>Şifreyi Değiştir
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        $('body').append(passwordModalHtml);
        $('#changePasswordModal').modal('show');

        $('#changePasswordModal').on('hidden.bs.modal', function () {
            $(this).remove();
        });
    },

    // Şifre değiştirme işlemi
    changePassword: function () {
        const currentPassword = $('#currentPassword').val();
        const newPassword = $('#newPassword').val();
        const confirmPassword = $('#confirmPassword').val();

        if (!currentPassword || !newPassword || !confirmPassword) {
            ToastHelper.warning('Tüm alanları doldurun!');
            return;
        }

        if (newPassword !== confirmPassword) {
            ToastHelper.error('Yeni şifreler eşleşmiyor!');
            return;
        }

        if (newPassword.length < 4) {
            ToastHelper.error('Yeni şifre en az 4 karakter olmalı!');
            return;
        }

        // AJAX ile şifre değiştirme (backend endpoint gerekli)
        LoaderHelper.show('Şifre değiştiriliyor...');

        $.ajax({
            url: '/Auth/ChangePassword',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                currentPassword: currentPassword,
                newPassword: newPassword
            }),
            success: function (response) {
                LoaderHelper.hide();
                if (response.success) {
                    ToastHelper.success('Şifre başarıyla değiştirildi!');
                    $('#changePasswordModal').modal('hide');
                } else {
                    ToastHelper.error(response.message || 'Şifre değiştirilemedi!');
                }
            },
            error: function () {
                LoaderHelper.hide();
                ToastHelper.error('Bağlantı hatası!');
            }
        });
    }
};

// Global fonksiyonlar (modal'lardan çağrılacak)
function openProfileModal() {
    ProfileManager.openProfileModal();
}

function openChangePasswordModal() {
    ProfileManager.openChangePasswordModal();
}

function showAppInfo() {
    ToastHelper.info('KufeArt Yönetim Sistemi v1.0 - 2025');
}

// Helper fonksiyonlar
function getCurrentUserName() {
    // Navbar'dan kullanıcı adını al
    const userNameElement = document.querySelector('.navbar-nav .dropdown-toggle span');
    return userNameElement ? userNameElement.textContent : 'Kullanıcı';
}

function getCurrentUsername() {
    // Claims'den username alınabilir, şimdilik statik
    return '@admin';
}