"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
const eth_sig_util_1 = __importDefault(require("eth-sig-util"));
function getCancelOrderEIP712Payload(orderHashes, salt, timestamp, chainId) {
    const payload = {
        types: {
            EIP712Domain: [
                { name: "name", type: "string" },
                { name: "version", type: "string" },
                { name: "chainId", type: "uint256" },
                { name: "salt", type: "bytes32" },
            ],
            Details: [
                { name: "orderHashes", type: "string[]" },
                { name: "timestamp", type: "uint256" },
            ],
        },
        primaryType: "Details",
        domain: {
            name: "CancelOrderV2SportX",
            version: "1.0",
            chainId,
            salt,
        },
        message: { orderHashes, timestamp },
    };
    return payload;
}
module.exports = function getCancelOrderV2Signature(callback, orderHashes, salt, timestamp, privateKey, chainId) {
    const payload = getCancelOrderEIP712Payload(orderHashes, salt, timestamp, chainId);
    const bufferPrivateKey = Buffer.from(privateKey.substring(2), "hex");
    const signature = eth_sig_util_1.default.signTypedData_v4(bufferPrivateKey, {
        data: payload,
    });
    callback(null, signature);
};
