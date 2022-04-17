# ProxyBind
An automated frontend for [3proxy](https://github.com/3proxy/3proxy), to setup an environment to redirect only desired programs, to go through VPN connection, achieving the functionality wise of a split tunneling setup

## Features

 1. Allow only desired programs e.g. web browsers to browse through VPN
 2. Unconfigured program will be using direct connection
 3. Allow multiple VPN connections running simultaneously
 4. Every web browser can be browsing through different VPN
 5. DNS queries are sent through VPN connection to specified resolver
 6. Auto-connect to specified Wi-Fi SSID (for WLAN without internet)
 7. Internet connection sharing over WLAN or LAN
 8. VPN connection sharing over WLAN or LAN
 9. No administrator right required

## Working principle
- Disable the creation of VPN gateway at system level
- Setup up a local web proxy server on the VPN TAP interface
- Thus only configured program will aware of the proxy

<br/><br/>

# Setup 
## Part 1/3: VPN and config file modification

### OpenVPN
**Config file**, by adding the following lines, they will disable the creation of VPN gateway at system level, but instead adding a very low priority gateway on the TAP interface
```ini
#disable default routing setup
route-nopull

#by using a very low priority metric
route-metric 9981

#adding a gateway on the TAP interface
route 0.0.0.0 0.0.0.0 vpn_gateway

#recommended to specify a TAP interface
dev-node Tap1
```
 
### OpenConnect
1. **Environment variable**, to disable the creation of VPN gateway at system level, add
	```console
	REDIRECT_GATEWAY_METHOD=1
	```
	
2. **`vpnc-script-win.js`** **file**,  to override the interface and gateway priorities, modify these lines
	```js
	//from
	run("netsh interface ip set interface " + env("TUNIDX") + " metric=1 store=active");
	//to
	run("netsh interface ip set interface " + env("TUNIDX") + " metric=9981 store=active");
	```

	```js
	//from
	run("route add 0.0.0.0 mask 0.0.0.0 " + internal_gw + " metric 1");
	//to
	run("route add 0.0.0.0 mask 0.0.0.0 " + internal_gw + " metric 9981");
	```

4. **Command line**, to specify TAP interface, add
	```console
	--interface=Tap2
	```
<br/>

## Part 2/3: ProxyBind
The release of ProxyBind should come with a default `.ini` file, which should be edited according to system:
```ini
[General]
#comma seperated
#3proxy external interface
sTapInterfaceName=Tap1, Tap2, Tap3, Wi-Fi 1, Wi-Fi 2
#3proxy internal IP
sIpInternal=0.0.0.0, 0.0.0.0, 0.0.0.0, 0.0.0.0
#DNS server
sIpDnsServer=1.1.1.1, 8.8.8.8, 9.9.9.9, 9.9.9.10
#HTTP proxy port
sPortProxy=16801, 16803, 16805, 16807, 16809
#SOCKS proxy port
sPortSocks=16802, 16804, 16806, 16808, 16810
#auto connect Wi-Fi interface
sAcwInterface=Wi-Fi 1, Wi-Fi 2
#auto connect Wi-Fi SSID
sAcwSsid=AP1, AP2_5Ghz

#auto-delete 3proxy config file
bDirCleanUp=True
#number of days to keep debugging log, set to 0 to disable logging
iLogDays=0
```
The example above will be creating following proxy servers:
| Proxy No. |External Interface |Internal IP |DNS Server | HTTP Proxy Port | SOCKS Proxy Port|
|-|-|-|-|-|-|
| 1 | Tap1 | 0.0.0.0 | 1.1.1.1 | 16801 | 16802 |
| 2 | Tap2 | 0.0.0.0 | 8.8.8.8 | 16803 | 16804 |
| 3 | Tap3 | 0.0.0.0 | 9.9.9.9 | 16805 | 16806 |
| 4 | Wi-Fi 1 | 0.0.0.0 | 9.9.9.10 | 16807 | 16808 |
| 5 | Wi-Fi 2 | 0.0.0.0 | 9.9.9.10 | 16809 | 16810 |

And will automatically connecting to Wi-Fi:
| SSID | via Interface |
|-|-|
| AP1 | Wi-Fi 1 |
| AP2_5Ghz | Wi-Fi 2 |

>Note 
>1. Interface `Wi-FI 1` and `Wi-FI 2` are not TAP interface, but desired program can still be configured to explicitly send traffics through these interface
>2. Internal IP `0.0.0.0` means all available local IP addresses, including loopback e.g. `127.0.0.1`
>3. Internal IP and DNS Server can be specified only once for the first proxy server, the following proxy will follow the settings from previous proxy
>4. If there is no intention to share connection over LAN, Internal IP can be set as `127.0.0.1` to avoid firewall prompt 
>5. Total number of auto-connect Wi-Fi is limited to total number of proxy will be creating
>6. Auto-connect Wi-Fi will work, provided that the SSID has been connected previously

<br/>

## Part 3/3: Configure program to use the proxy server
Many programs support proxy connection, especially web browser, e.g. Chrome, Edge, Firefox and Brave have extension to manage their proxy settings. Cloud service e.g. Dropbox and MEGA, remote access service over WAN e.g. TeamViewer and AnyDesk, all come with built-in configurable proxy setting

<br/><br/>

# Know more about
- The program which is creating the proxy server: [3proxy](https://github.com/3proxy/3proxy)
- The program which is handling the DNS queries: [UDP Forwarder CLI](https://github.com/qitamic/udpfwdc)
- An easy to use proxy extension for [Chrome/Edge/Brave](https://chrome.google.com/webstore/detail/proxy-switcher-and-manage/onnfghpihccifgojkpnnncpagjcdbjod), [Edge](https://microsoftedge.microsoft.com/addons/detail/proxy-switcher-and-manage/gneeeeckemnjlgopgpchamgmfpkglgaj), and [Firefox](https://addons.mozilla.org/en-US/firefox/addon/proxy-switcher-and-manager/)

<br/><br/>
