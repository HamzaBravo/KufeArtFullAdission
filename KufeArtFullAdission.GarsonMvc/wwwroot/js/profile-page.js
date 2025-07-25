// KufeArtFullAdission.GarsonMvc/wwwroot/js/profile-page.js
class ProfilePage {
    constructor() {
        this.bindEvents();
    }

    bindEvents() {
        // Modal dışı tıklama
        document.getElementById('passwordModalOverlay').addEventListener('click', (e) => {
            if (e.target === e.currentTarget) {
                this.closePasswordModal();
            }
        });

        // ESC tuşu ile modal kapatma
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closePasswordModal();
            }
        });
    }

    // Ad soyad düzenleme
    editFullName() {
        const displayElement = document.getElementById('displayFullName');
        const currentName = displayElement.textContent;

        const input = document.createElement('input');
        input.type = 'text';
        input.value = currentName;
        input.className = 'edit-input';
        input.maxLength = 100;

        // Save/Cancel buttons
        const actions = document.createElement('div');
        actions.className = 'edit-actions';
        actions.innerHTML = `
            <button class="btn-save" onclick="saveFullName(this, '${currentName}')">
                <i class="fas fa-check"></i>
            </button>
            <button class="btn-cancel" onclick="cancelEdit('displayFullName', '${currentName}')">
                <i class="fas fa-times"></i>
            </button>
        `;

        displayElement.replaceWith(input);
        input.parentNode.querySelector('.btn-edit').replaceWith(actions);
        input.focus();
        input.select();
    }

    async saveFullName(button, originalName) {
        const input = document.querySelector('.edit-input');
        const newName = input.value.trim();

        if (!newName) {
            this.showError('Ad soyad boş olamaz!');
            return;
        }

        if (newName === originalName) {
            this.cancelEdit('displayFullName', originalName);
            return;
        }

        try {
            button.disabled = true;
            button.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';

            const response = await fetch('/Profile/UpdateProfile', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    fullName: newName
                })
            });

            const result = await response.json();

            if (result.success) {
                // UI güncelle
                const displayElement = document.createElement('span');
                displayElement.id = 'displayFullName';
                displayElement.textContent = newName;

                const editButton = document.createElement('button');
                editButton.className = 'btn-edit';
                editButton.onclick = () => this.editFullName();
                editButton.innerHTML = '<i class="fas fa-edit"></i>';

                input.replaceWith(displayElement);
                button.parentNode.replaceWith(editButton);

                // Header'daki ismi de güncelle
                document.getElementById('profileName').textContent = newName;

                this.showSuccess(result.message);
            } else {
                this.showError(result.message);
                button.disabled = false;
                button.innerHTML = '<i class="fas fa-check"></i>';
            }
        } catch (error) {
            this.showError('Bağlantı hatası!');
            button.disabled = false;
            button.innerHTML = '<i class="fas fa-check"></i>';
        }
    }

    cancelEdit(elementId, originalValue) {
        const input = document.querySelector('.edit-input');
        const actions = document.querySelector('.edit-actions');

        const displayElement = document.createElement('span');
        displayElement.id = elementId;
        displayElement.textContent = originalValue;

        const editButton = document.createElement('button');
        editButton.className = 'btn-edit';
        editButton.onclick = () => this.editFullName();
        editButton.innerHTML = '<i class="fas fa-edit"></i>';

        input.replaceWith(displayElement);
        actions.replaceWith(editButton);
    }

    // Şifre değiştirme modal
    openChangePasswordModal() {
        document.getElementById('passwordModalOverlay').style.display = 'flex';
        document.body.style.overflow = 'hidden';
        setTimeout(() => {
            document.getElementById('currentPassword').focus();
        }, 100);
    }

    closePasswordModal() {
        document.getElementById('passwordModalOverlay').style.display = 'none';
        document.body.style.overflow = '';
        document.getElementById('changePasswordForm').reset();
        this.resetPasswordButton();
    }

    async changePassword(event) {
        event.preventDefault();

        const currentPassword = document.getElementById('currentPassword').value;
        const newPassword = document.getElementById('newPassword').value;
        const confirmPassword = document.getElementById('confirmPassword').value;

        // Validasyon
        if (newPassword !== confirmPassword) {
            this.showError('Yeni şifreler eşleşmiyor!');
            return;
        }

        if (newPassword.length < 4) {
            this.showError('Yeni şifre en az 4 karakter olmalıdır!');
            return;
        }

        if (newPassword === currentPassword) {
            this.showError('Yeni şifre eskisiyle aynı olamaz!');
            return;
        }

        this.setPasswordButtonLoading(true);

        try {
            const response = await fetch('/Profile/ChangePassword', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    currentPassword: currentPassword,
                    newPassword: newPassword
                })
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message);
                this.closePasswordModal();
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            this.showError('Bağlantı hatası!');
        } finally {
            this.setPasswordButtonLoading(false);
        }
    }

    setPasswordButtonLoading(loading) {
        const button = document.getElementById('changePasswordBtn');
        const btnText = button.querySelector('.btn-text');
        const btnLoading = button.querySelector('.btn-loading');

        if (loading) {
            btnText.style.display = 'none';
            btnLoading.style.display = 'flex';
            button.disabled = true;
        } else {
            btnText.style.display = 'flex';
            btnLoading.style.display = 'none';
            button.disabled = false;
        }
    }

    resetPasswordButton() {
        const button = document.getElementById('changePasswordBtn');
        const btnText = button.querySelector('.btn-text');
        const btnLoading = button.querySelector('.btn-loading');

        btnText.style.display = 'flex';
        btnLoading.style.display = 'none';
        button.disabled = false;
    }

    // Şifre görünürlük toggle
    togglePasswordVisibility(inputId) {
        const input = document.getElementById(inputId);
        const icon = input.parentNode.querySelector('.password-toggle i');

        if (input.type === 'password') {
            input.type = 'text';
            icon.classList.replace('fa-eye', 'fa-eye-slash');
        } else {
            input.type = 'password';
            icon.classList.replace('fa-eye-slash', 'fa-eye');
        }
    }

    // Çıkış onayı
    confirmLogout() {
        if (confirm('Çıkış yapmak istediğinizden emin misiniz?')) {
            window.location.href = '/Auth/Logout';
        }
    }

    // Toast messages
    showSuccess(message) {
        this.showToast(message, 'success');
    }

    showError(message) {
        this.showToast(message, 'error');
    }

    showToast(message, type) {
        // Basit toast sistemi
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.innerHTML = `
            <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
            ${message}
        `;

        document.body.appendChild(toast);

        setTimeout(() => {
            toast.classList.add('show');
        }, 100);

        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }
}

// Global functions
function editFullName() {
    window.profilePage.editFullName();
}

function saveFullName(button, originalName) {
    window.profilePage.saveFullName(button, originalName);
}

function cancelEdit(elementId, originalValue) {
    window.profilePage.cancelEdit(elementId, originalValue);
}

function openChangePasswordModal() {
    window.profilePage.openChangePasswordModal();
}

function closePasswordModal() {
    window.profilePage.closePasswordModal();
}

function changePassword(event) {
    window.profilePage.changePassword(event);
}

function togglePasswordVisibility(inputId) {
    window.profilePage.togglePasswordVisibility(inputId);
}

function confirmLogout() {
    window.profilePage.confirmLogout();
}

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    window.profilePage = new ProfilePage();
});