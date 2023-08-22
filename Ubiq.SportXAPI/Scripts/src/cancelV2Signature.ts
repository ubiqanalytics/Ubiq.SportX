import ethSigUtil from "eth-sig-util";

function getCancelOrderEIP712Payload(orderHashes: string[], salt: string, timestamp: number, chainId: number) {
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

export = function getCancelOrderV2Signature(
  callback: any,
  orderHashes: string[],
  salt: string,
  timestamp: number,
  privateKey: string,
  chainId: number
) {
  const payload = getCancelOrderEIP712Payload(orderHashes, salt, timestamp, chainId);
  const bufferPrivateKey = Buffer.from(privateKey.substring(2), "hex");
  const signature = (ethSigUtil as any).signTypedData_v4(bufferPrivateKey, {
    data: payload,
  });
  callback(null, signature);
};
