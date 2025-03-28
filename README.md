# Smart Receptionist Project Setup Guide

## Table of Contents

- [Website](#website)
  - [Prerequisites](#prerequisites)
  - [Add a new subscription](#add-a-new-subscription)
  - [Create an Azure Active Directory (AAD) tenant](#create-an-azure-active-directory-aad-tenant)
  - [Register a new application](#register-a-new-application)
  - [Final steps](#final-steps)
  - [Web.config](#webconfig)
- [Alexa Skills Kit (ASK) Developer Console](#alexa-skills-kit-ask-developer-console)
  - [Create the Smart Receptionist skill](#create-the-smart-receptionist-skill)
- [AWS Lambda](#aws-lambda)
  - [Prerequisites](#prerequisites-1)
  - [Create an IAM user with programmatic access](#create-an-iam-user-with-programmatic-access)
  - [Publish to AWS Lambda](#publish-to-aws-lambda)
  - [Add the ASK trigger](#add-the-ask-trigger)

## Website

### Prerequisites

1. Install the [.NET Framework 4.7.1](https://www.microsoft.com/en-us/download/details.aspx?id=56116).
2. Clone the [TFS repository](http://fypjtfs.sit.nyp.edu.sg/tfs/DefaultCollection/_git/SmartReceptionist).

### Add a new subscription

1. Sign in to the [Microsoft Azure portal](https://portal.azure.com/).
2. At the top-left, click **All services**. Under the "General" category, click **Subscriptions**.
3. Click **Add**.
4. Select **Free Trial** and follow the steps to sign up for a free Azure account.
5. Once done, return to the Azure portal.

### Create an Azure Active Directory (AAD) tenant

To create an AAD tenant, follow steps 1–6 [here](https://docs.microsoft.com/en-us/power-bi/developer/create-an-azure-active-directory-tenant#create-an-azure-active-directory-tenant).

### Register a new application

1. At the top-left, click **All services**. Under the "Identity" category, click **App registrations**.
2. Click **New application registration**.

   ![image](https://github.com/user-attachments/assets/6cf6647b-b97e-4dd0-a3ed-a3a371fa6cba)

3. Enter the name of the application and sign-on URL and click **Create**.

   ![image](https://github.com/user-attachments/assets/9459403b-7acd-4aaa-bb4c-cc9c5bfd9c4a)

4. Copy and store the application ID somewhere. We'll need it for configuring `web.config` later.
5. Next, click **Settings** $\longrightarrow$ **Keys**.
6. Create a new public key by specifying the key description and duration. Click **Save**.
7. Copy and store the key value somewhere. We'll need it for configuring `web.config` later.

### Final steps

1. Click **Azure Active Directory** at the left sidebar under "Favorites."
2. Click **Custom domain names**.
3. Copy the primary domain shown and store it somewhere.

   ![image](https://github.com/user-attachments/assets/272b636a-6025-4787-8818-2d81edb6af3c)

   > **Note:** Your primary domain name will be _domainname_.onmicrosoft.com where _domainname_ is the initial domain name you specified when creating an AAD tenant.

4. Lastly, click **properties** and copy the **Directory ID**.

### Web.config

Open `web.config` and replace the following lines with the values you copied and stored earlier:

Line 22: `application_id`

Line 24: `key_value`

Line 25: `primary_domain_name`

Line 26: `directory_id`

## Alexa Skills Kit (ASK) Developer Console

### Create the Smart Receptionist skill

1. Sign in to the [ASK Developer Console](https://developer.amazon.com/alexa/console/ask).
2. Click **Create Skill**, and on the next screen, specify the skill name as **Smart Receptionist**.
3. Click **Next**, select the **Custom** model and then create the skill.
4. Under the "Interaction Model" on the left, click **JSON Editor**.
5. Import the `model.json` file [here](model.json) and build the model.
6. Lastly, click the "Interfaces" header on the left and enable "Display Interface".
7. Save the interface and build the model to complete.

## AWS Lambda

### Prerequisites

1. Install the [AWS Toolkit for Visual Studio](https://aws.amazon.com/visualstudio/).
2. [Create an AWS account](https://portal.aws.amazon.com/gp/aws/developer/registration/index.html?nc2=h_ct).

### Create an IAM user with programmatic access

1. Sign in to the [IAM Management Console](https://console.aws.amazon.com/iam/home).
2. Under the "IAM Resources" section, click **Users**.

   ![image](https://github.com/user-attachments/assets/ad8e682c-4972-4a7a-b1c2-bbf53e52c78b)

3. Next, click **Add user**.
4. Enter a username and set the access type to **programmatic access**. Click **Next: Permissions**.

   ![image](https://github.com/user-attachments/assets/585d7b62-1876-42d1-b041-0549c57c86fb)

5. Assign the user administrator access by attaching the existing **AdministratorAccess** policy. Click **Next: Review**.

   ![image](https://github.com/user-attachments/assets/097ad57e-f8be-4042-8997-db312d9cdea9)

6. Create the user once you have reviewed everything is correct.
7. Copy the access key ID and secret access key. Use those values to create a new account profile after installing the AWS Toolkit for Visual Studio.
8. Click [here](https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/getting-set-up.html) for more information.

### Publish to AWS Lambda

To publish your function to AWS Lambda, follow the instructions [here](https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/lambda-creating-project-in-visual-studio.html#publish-to-lam).

> [!NOTE]
> When publishing your function, change your region to US East (N. Virginia) as the ASK trigger is only available in that region.

### Add the ASK trigger

After publishing your function, you'll need to add the ASK trigger.

1. Go to the [Lambda Management Console](https://console.aws.amazon.com/lambda/home).
2. Make sure your region is set to **N. Virginia**. If not, click the dropdown at the top-right beside your name, and click **N. Virginia**.

   ![image](https://github.com/user-attachments/assets/0a6f21bf-b9cb-42cf-a068-77ca9a0d60bd)

3. Click the function which you have published previously.

   ![image](https://github.com/user-attachments/assets/6311b09c-a2b4-400e-953a-496501cb7a57)

4. In the "Designer" section, under the "Add triggers" pane, click **Alexa Skills Kit**.
5. Next, in the "Configure triggers" section, enter the Smart Receptionist skill ID. You can find the skill ID in the [ASK Developer Console](https://developer.amazon.com/alexa/console/ask), just below the skill name.

   ![image](https://github.com/user-attachments/assets/3fb5ec77-7afd-4100-b689-7c67bb3fc5aa)

6. Click **save** to complete.
