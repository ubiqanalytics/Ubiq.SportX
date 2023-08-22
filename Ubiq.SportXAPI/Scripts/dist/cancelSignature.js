"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
const eth_sig_util_1 = __importDefault(require("eth-sig-util"));
function getCancelOrderEIP712Payload(orders, chainId) {
    const payload = {
        types: {
            EIP712Domain: [
                { name: "name", type: "string" },
                { name: "version", type: "string" },
                { name: "chainId", type: "uint256" },
            ],
            Details: [
                { name: "message", type: "string" },
                { name: "orders", type: "string[]" },
            ],
        },
        primaryType: "Details",
        domain: {
            name: "CancelOrderSportX",
            version: "1.0",
            chainId,
        },
        message: {
            orders,
            message: "Are you sure you want to cancel these orders",
        },
    };
    return payload;
}
module.exports = function getCancelOrderSignature(callback, orders, privateKey, chainId) {
    const payload = getCancelOrderEIP712Payload(orders, chainId);
    const bufferPrivateKey = Buffer.from(privateKey.substring(2), "hex");
    const signature = eth_sig_util_1.default.signTypedData_v4(bufferPrivateKey, {
        data: payload,
    });
    callback(null, signature);
};
