CryptoNote-Easy-Miner for DigitalNote (XDN)
===

This is a simple C# app that helps Windows users start mining without dealing with command-line operated binaries. It is bundled with the latest 32 & 64 bit builds of cpuminer and simplewallet.


Upon starting for the first time it will run simplewallet to generate a new address (with a default wallet password of `x`). The user can then input a pool host & port, select how many CPU cores they want to use, the click `Start Mining`.


The app will spawn instance of cpuminer for each core with the appropriate command-line arguments.

####Note

Please keep xdn.wallet file safe. This is your wallet with default password `x`

####Download

Get the latest build here: [releases](//github.com/zone117x/cryptonote-easy-miner/releases)
