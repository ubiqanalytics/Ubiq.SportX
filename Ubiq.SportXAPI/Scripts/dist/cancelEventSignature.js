"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
const eth_sig_util_1 = __importDefault(require("eth-sig-util"));
function getCancelOrderEventsEIP712Payload(sportXeventId, salt, timestamp, chainId) {
    const payload = {
        types: {
            EIP712Domain: [
                { name: "name", type: "string" },
                { name: "version", type: "string" },
                { name: "chainId", type: "uint256" },
                { name: "salt", type: "bytes32" },
            ],
            Details: [
                { name: "sportXeventId", type: "string" },
                { name: "timestamp", type: "uint256" },
            ],
        },
        primaryType: "Details",
        domain: {
            name: "CancelOrderEventsSportX",
            version: "1.0",
            chainId,
            salt,
        },
        message: { sportXeventId, timestamp },
    };
    return payload;
}
module.exports = function getCancelEventOrdersSignature(callback, sportXeventId, salt, timestamp, privateKey, chainId) {
    const payload = getCancelOrderEventsEIP712Payload(sportXeventId, salt, timestamp, chainId);
    const bufferPrivateKey = Buffer.from(privateKey.substring(2), "hex");
    const signature = eth_sig_util_1.default.signTypedData_v4(bufferPrivateKey, {
        data: payload,
    });
    callback(null, signature);
};
