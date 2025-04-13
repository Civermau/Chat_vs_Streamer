# Twitch API Usage

## Authorization URL

```bash
https://id.twitch.tv/oauth2/authorize
    ?response_type=token
    &client_id=<YOUR_CLIENT_ID>
    &redirect_uri=<YOUR_REDIRECT_URI>
    &scope=channel:manage:redemptions+bits:read
```

## Create Custom Reward

```bash
curl -X POST 'https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id=<YOUR_BROADCASTER_ID>' \
-H 'client-id: <YOUR_CLIENT_ID>' \
-H 'Authorization: Bearer <YOUR_ACCESS_TOKEN>' \
-H 'Content-Type: application/json' \
-d '{
  "title":"test reward with curl",
  "cost":2
}'
```

## Delete Custom Reward

```bash
curl -X DELETE 'https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id=<YOUR_BROADCASTER_ID>&id=<YOUR_REWARD_ID>' \
-H 'Client-Id: <YOUR_CLIENT_ID>' \
-H 'Authorization: Bearer <YOUR_ACCESS_TOKEN>'
```

## Get Custom Rewards

```bash
curl -X GET 'https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id=<YOUR_BROADCASTER_ID>' \
-H 'Client-Id: <YOUR_CLIENT_ID>' \
-H 'Authorization: Bearer <YOUR_ACCESS_TOKEN>'
```

All of this can be made in Postman too though.

![image](https://github.com/user-attachments/assets/6640621f-1590-4d00-84e0-d588e2e80078)
![image](https://github.com/user-attachments/assets/6519db70-35ae-4ece-849b-230d0400bbae)
