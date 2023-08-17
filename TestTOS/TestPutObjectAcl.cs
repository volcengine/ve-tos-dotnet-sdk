using NUnit.Framework;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{ 
    [TestFixture]
    public class TestPutObjectAcl
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("put-obj-acl-basic");
            var key = Util.GenerateObjectName("normal");
            var data = "hello world";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
            
            var putObjectInput = new PutObjectInput()
            {
                Bucket = bucket,
                Key = key,
                Content = Util.ConvertStringToStream(data),
                ACL = ACLType.ACLPublicRead
            };
            var putObjectOutput = client.PutObject(putObjectInput);
            Assert.Greater(putObjectOutput.RequestID.Length, 0);                
            Assert.Greater(putObjectOutput.ETag.Length, 0);                
            Assert.AreEqual(200, putObjectOutput.StatusCode);

            var getObjectAclInput = new GetObjectACLInput()
            {
                Bucket = bucket,
                Key = key
            };
            var getObjectAclOutput = client.GetObjectACL(getObjectAclInput);
            Assert.AreEqual(1, getObjectAclOutput.Grants.Length);
            Assert.AreEqual(PermissionType.PermissionRead, getObjectAclOutput.Grants[0].Permission);
            Assert.AreEqual(GranteeType.GranteeGroup, getObjectAclOutput.Grants[0].Grantee.Type);
            Assert.AreEqual(CannedType.CannedAllUsers, getObjectAclOutput.Grants[0].Grantee.Canned);

            var putObjectAclInput = new PutObjectACLInput()
            {
                Bucket = bucket,
                Key = key,
                ACL = ACLType.ACLPublicReadWrite
            };
            var putObjectAclOutput = client.PutObjectACL(putObjectAclInput);
            Assert.Greater(putObjectAclOutput.RequestID.Length, 0); 
            
            getObjectAclOutput = client.GetObjectACL(getObjectAclInput);
            Assert.AreEqual(2, getObjectAclOutput.Grants.Length);
            foreach (var grant in getObjectAclOutput.Grants)
            {
                Assert.True(grant.Permission == PermissionType.PermissionRead || grant.Permission == PermissionType.PermissionWrite);
                Assert.AreEqual(GranteeType.GranteeGroup, grant.Grantee.Type);
                Assert.AreEqual(CannedType.CannedAllUsers, grant.Grantee.Canned);
            }

            var ownerId = getObjectAclOutput.Owner.ID;
            putObjectAclInput = new PutObjectACLInput()
            {
                Bucket = bucket,
                Key = key,
                Owner = new Owner() { ID = ownerId },
                Grants = new [] {
                    new Grant()
                    {
                        Grantee = new Grantee()
                        {
                            ID = "",
                            Type = GranteeType.GranteeGroup,
                            Canned = CannedType.CannedAuthenticatedUsers
                        },
                        Permission = PermissionType.PermissionRead
                    }
                }
            };
            putObjectAclOutput = client.PutObjectACL(putObjectAclInput);
            Assert.Greater(putObjectAclOutput.RequestID.Length, 0); 

            getObjectAclOutput = client.GetObjectACL(getObjectAclInput);
            Assert.AreEqual(1, getObjectAclOutput.Grants.Length);
            Assert.AreEqual(PermissionType.PermissionRead, getObjectAclOutput.Grants[0].Permission);
            Assert.AreEqual(GranteeType.GranteeGroup, getObjectAclOutput.Grants[0].Grantee.Type);
            Assert.AreEqual(CannedType.CannedAuthenticatedUsers, getObjectAclOutput.Grants[0].Grantee.Canned);
            
            putObjectAclInput = new PutObjectACLInput()
            {
                Bucket = bucket,
                Key = key,
                Owner = new Owner() { ID = ownerId },
                Grants = new [] {
                    new Grant()
                    {
                        Grantee = new Grantee()
                        {
                            ID = ownerId,
                            Type = GranteeType.GranteeUser,
                        },
                        Permission = PermissionType.PermissionFullControl
                    }
                }
            };
            putObjectAclOutput = client.PutObjectACL(putObjectAclInput);
            Assert.Greater(putObjectAclOutput.RequestID.Length, 0); 

            getObjectAclOutput = client.GetObjectACL(getObjectAclInput);
            Assert.AreEqual(1, getObjectAclOutput.Grants.Length);
            Assert.AreEqual(PermissionType.PermissionFullControl, getObjectAclOutput.Grants[0].Permission);
            Assert.AreEqual(GranteeType.GranteeUser, getObjectAclOutput.Grants[0].Grantee.Type);
            Assert.AreEqual(ownerId, getObjectAclOutput.Grants[0].Grantee.ID);
        }

        [Test]
        public void TestAbnormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("put-obj-acl-basic");
            var key = Util.GenerateObjectName("abnormal");
            var data = "hello world";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));


            var putObjectAclInput = new PutObjectACLInput()
            {
                Bucket = "no-exist-bucket",
                Key = key,
                ACL = ACLType.ACLPublicReadWrite
            };
            
            var ex = Assert.Throws<TosServerException>(() => client.PutObjectACL(putObjectAclInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);
            
            putObjectAclInput = new PutObjectACLInput()
            {
                Bucket = bucket,
                Key = "no-exist-key",
                ACL = ACLType.ACLPublicReadWrite
            };
            
            ex = Assert.Throws<TosServerException>(() => client.PutObjectACL(putObjectAclInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchKey", ex.Code);
            
            var getObjectAclInput = new GetObjectACLInput()
            {
                Bucket = "no-exist-bucket",
                Key = key,
            };
            
            ex = Assert.Throws<TosServerException>(() => client.GetObjectACL(getObjectAclInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);
            
            getObjectAclInput = new GetObjectACLInput()
            {
                Bucket = bucket,
                Key = "no-exist-key",
            };
            
            ex = Assert.Throws<TosServerException>(() => client.GetObjectACL(getObjectAclInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchKey", ex.Code);
        }
    }
}