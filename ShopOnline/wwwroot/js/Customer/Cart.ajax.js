﻿const ROUTE = {
    CART: {
        ADD: '/Cart/AddProductToCart',
        REDUCE: '/Cart/ReduceProductFromCart',
        REMOVE: '/Cart/RemoveProductFromCart',
        REMOVE_ALL: '/Cart/RemoveAllProductFromCart',
        PRODUCT_CART: '/Cart/ProductCart'
    },
}

const ID_ELEMENT = {
    QUANTITY_PRODUCT_CART: 'quantity-product-cart',
}

const FORM_ID = {
    ADD_TO_CART: '#add-to-cart-form',
    REGISTER: '#register-form',
    RESET_PASSWORD: '#reset-password-form',
}

//#region Add to cart
function addToCart() {
    let formData = $(FORM_ID.ADD_TO_CART).serializeArray();

    $.ajax({
        url: ROUTE.CART.ADD,
        type: 'post',
        contentType: "application/x-www-form-urlencoded",
        data: formData,
        success: function () {
            window.location.reload();
        },
        error: function (XMLHttpRequest) {
            //const EleErrorMsgLogin = {
            //    Email: 'email_msg',
            //    Password: 'password_msg',
            //}

            //let msg = XMLHttpRequest.responseJSON ? XMLHttpRequest.responseJSON : XMLHttpRequest.responseText;

            //innerHTMLMsg(msg, EleErrorMsgLogin);
        },
    });
}

function addMoreToCart(id) {
    const formData = {
        idProduct: id,
        quantity: 1
    }

    $.ajax({
        url: ROUTE.CART.ADD,
        type: 'post',
        contentType: "application/x-www-form-urlencoded",
        data: formData,
        success: function () {
            window.location.reload();
        },
        error: function (XMLHttpRequest) {

        },
    });
}
//#endregion

//#region Remove from cart
function reduceFromCart(id) {
    const formData = {
        idProduct: id,
        quantity: 1
    }

    $.ajax({
        url: ROUTE.CART.REDUCE,
        type: 'post',
        contentType: "application/x-www-form-urlencoded",
        data: formData,
        success: function () {
            window.location.reload();
        },
        error: function (XMLHttpRequest) {

        },
    });
}
//#endregion

//#region Remove from cart
function removeFromCart(id) {
    const formData = {
        idProduct: id
    }

    $.ajax({
        url: ROUTE.CART.REMOVE,
        type: 'post',
        contentType: "application/x-www-form-urlencoded",
        data: formData,
        success: function () {
            window.location.reload();
        },
        error: function (XMLHttpRequest) {

        },
    });
}
//#endregion