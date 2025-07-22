// wwwroot/js/globals.js
window.App = {
    // Mevcut global değişkenler
    currentTableOrders: [],
    currentTablesData: {},
    currentProducts: [],
    cartItems: [],
    currentTableId: null,
    currentTableRemainingAmount: 0,

    // İşlem kontrolleri
    isPaymentProcessing: false,

    // API endpoints
    endpoints: {
        getTables: '/Home/GetTables',
        getTableDetails: '/Home/GetTableDetails',
        processQuickPayment: '/Home/ProcessQuickPayment',
        submitOrder: '/Home/SubmitOrder',
        getProducts: '/Home/GetProducts',
        openTableAccount: '/Home/OpenTableAccount',
        closeTableAccount: '/Home/CloseTableAccount'
    }
};